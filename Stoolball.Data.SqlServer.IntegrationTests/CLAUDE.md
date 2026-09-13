# Authoring integration tests in Stoolball.Data.SqlServer.IntegrationTests

This project tests the SQL Server repository implementations against a real database.
Test data generation lives in the separate `Stoolball.Testing` project (it depends on
Bogus, so that dependency is kept out of production code).

## 1. Isolating each test with TransactionScope

Every test class (or a shared `*TestsBase` class for repositories split one-file-per-method)
opens a `System.Transactions.TransactionScope` in its constructor and disposes it in
`Dispose()`. Nothing is ever committed by the test itself — disposing an un-completed
scope rolls back everything the test did.

```csharp
[Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
public class SqlServerClubRepositoryTests : IDisposable
{
    private readonly SqlServerTestDataFixture _databaseFixture;
    private readonly TransactionScope _scope;

    public SqlServerClubRepositoryTests(SqlServerTestDataFixture databaseFixture)
    {
        _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        _scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
    }

    public void Dispose() => _scope.Dispose();
}
```

See `Stoolball.Data.SqlServer.IntegrationTests\Clubs\SqlServerClubRepositoryTests.cs` and
`Matches\SqlServerMatchRepositoryTests\MatchRepositoryTestsBase.cs` for the pattern applied
to a repository split across multiple test files.

**Gotcha — this only works if the repository under test enlists in the ambient transaction.**
Plain `IDbConnection.BeginTransaction()` does **not** automatically join an ambient
`TransactionScope`. Repository code must call the extension method instead:

```csharp
// Stoolball.Data.SqlServer\DbConnectionTransactionExtensions.cs
public static IDbTransaction? BeginTransactionIfNoAmbientTransaction(this IDbConnection connection)
{
    // Returns null when a TransactionScope is already ambient (as in these tests) —
    // the connection enlists in that instead, and callers must Commit()/Rollback()
    // via the null-conditional operator since the ambient scope controls the outcome.
    return Transaction.Current == null ? connection.BeginTransaction() : null;
}
```

If you add a new repository method that opens its own transaction, use
`connection.BeginTransactionIfNoAmbientTransaction()`, not `connection.BeginTransaction()`,
or changes will leak between tests despite the `TransactionScope`.

The baseline seed data (below) is committed once per test **collection**, outside any
scope, so it is never rolled back — only what an individual test does inside its own
`TransactionScope` is undone.

## 2. Seeded Bogus data for repeatable bulk volumes

`SqlServerTestDataFixture` (an xUnit `ICollectionFixture`, one instance per test collection)
builds the whole baseline dataset once by fixing Bogus's static seed immediately before
generating it:

```csharp
// Fixtures\SqlServerTestDataFixture.cs
using var serviceProvider = new ServiceCollection().AddSeedDataGenerator().BuildServiceProvider();
var randomSeedDataGenerator = serviceProvider.GetRequiredService<SeedDataGenerator>();

Randomizer.Seed = new Random(85437684);
TestData = randomSeedDataGenerator.GenerateTestData();
```

`Randomizer.Seed` is global to the process, so fixing it here makes every `Faker<T>` used
by every factory deterministic for the rest of that run — the same teams, competitions,
players, matches etc. are produced on every test run. This is what makes it safe to write
assertions like "there are exactly N competitions" or to rely on `TestData.TeamWithFullDetails`
having consistent shape.

Use a fixed seed when you want **volume and variety of realistic-looking data that doesn't
change under you** — bulk fixtures, pagination tests, anything that isn't specifically
testing for the presence of an edge case.

## 3. Unseeded randomness for fuzz-style testing

Not everything should be pinned down. `SeedDataGenerator` and the factories also draw on
`Randomiser` (`Stoolball.Testing\Randomiser.cs`, a thin wrapper over `System.Random`),
which is deliberately registered **unseeded**:

```csharp
// Stoolball.Testing\ServiceCollectionExtensions.cs
services.AddSingleton(new Randomiser(new Random()));
```

`Randomiser` drives structural decisions — `IsEven`, `OneInFourChance`, `FiftyFiftyChance`,
`PositiveIntegerLessThan`, `Between` — so *which* fields are null, how many innings a match
has, whether a location exists, etc. varies from run to run even when Bogus's fake values
are seeded. This is a deliberate fuzzing device: it exercises combinations you didn't think
to write down explicitly, and it will occasionally turn up cases the code doesn't handle.

The pattern for writing a fuzz-style test is: run generation N times with **no fixed seed**,
and assert an invariant holds on every iteration. See
`Stoolball.UnitTests\Testing\SeedDataGeneratorTests.cs`:

```csharp
// Run each test enough times to be confident random data generation always matches the test
private const int _iterations = 10;

[Fact]
public void Five_wicket_haul_exists()
{
    var generator = CreateGenerator();
    for (var i = 0; i < _iterations; i++)
    {
        var teams = generator.GenerateTeams();
        var innings = generator.GenerateMatchData(new TestData(), teams).SelectMany(x => x.MatchInnings);
        Assert.True(innings.Any(x => /* invariant under test */));
    }
}
```

Other examples of the same unseeded-random idea: `ListOfStringExtensions.ChangeCaseAndSometimesTrimOneEnd`
and `DateRangeGenerator.SelectDateRangeToTest` (both use their own unseeded `new Random()`).

**When a fuzz run finds a failure**, don't just fix the code and move on — turn what it found
into a permanent, named regression test. Usually that means: pin the specific shape of data
that broke things using a factory's optional parameters (see §4) or a new `*DataProvider`
scenario (see §5), so the case is covered every run without depending on randomly rolling it
again. There should always be at least one provider/test in the suite that runs with a
genuinely unseeded `Random`/`Randomiser`, so new edge cases keep surfacing over time — don't
accidentally seed away the last one.

## 4. Factories: the preferred building block (not SeedDataGenerator)

`Stoolball.Testing\SeedDataGenerator.cs` is a large, actively-shrinking "God class" that
originally did everything. **New test data generation should not be added to it.** Instead:

- A **Factory** (`Stoolball.Testing\Factories\*Factory.cs`) wraps a `Faker<T>` for one
  entity type, exposed via a `CreateFaker(...)` method. Optional parameters let a caller pin
  specific fields while leaving the rest random — this is the idiom for "give me a season
  that starts in 2020" rather than writing a bespoke `CreateXWithY` method:

  ```csharp
  // Stoolball.Testing\Factories\SeasonFactory.cs
  public class SeasonFactory(OverSetFactory _overSetFactory)
  {
      /// <param name="fromYear">Fixes the season's start year instead of randomising it.</param>
      /// <param name="untilYear">Fixes the season's end year instead of randomising it.</param>
      public Faker<Season> CreateFaker(Competition competition, int? fromYear = null, int? untilYear = null)
      {
          return new Faker<Season>()
                  .RuleFor(x => x.FromYear, faker => fromYear ?? faker.Random.Int(2000, 2025))
                  .RuleFor(x => x.UntilYear, (faker, season) => untilYear ?? season.FromYear + faker.Random.Int(0, 1))
                  ...
      }
  }
  ```

  A factory can also hold small non-Faker helper builders for the same entity, e.g.
  `OverFactory.CreateOversBowledIncludingOneWithOnlyName(...)` alongside its `CreateFaker(...)`.

- A **Provider** (`Stoolball.Testing\MatchDataProviders`, `CompetitionDataProviders`,
  `PlayerDataProviders`, `SchoolDataProviders`) composes several factories to build one
  deliberately-shaped, named edge-case scenario, given the data generated so far:

  ```csharp
  // Stoolball.Testing\MatchDataProviders\BaseMatchDataProvider.cs
  internal abstract class BaseMatchDataProvider
  {
      internal abstract IEnumerable<Match> CreateMatches(TestData readOnlyTestData);
  }
  ```

  Concrete providers are named after the scenario they cover, e.g.
  `APlayerWithTwoIdentitiesOnOneTeamTakesFiveWicketsOnlyWhenBothAreCombined`,
  `MatchesInTheFuture`, `EveryMatchResultType`. When a fuzz run (§3) turns up a case worth
  keeping permanently, write it as a new provider like these rather than as a one-off inline
  block in `SeedDataGenerator`.

- **"Faker"** is the informal name for the `Faker<T>` a factory's `CreateFaker(...)` returns —
  there isn't a separate `*Faker` class type, `*Factory` is the class, its output is the faker.

Providers are still invoked from `SeedDataGenerator.GenerateTestData()` as plain arrays
(not resolved via DI, since each scenario needs a different combination of constructor
arguments per call):

```csharp
var matchProviders = new BaseMatchDataProvider[]{
    new APlayerOnlyWinsAnAwardButHasPlayedOtherMatchesWithADifferentTeam(_randomiser, _matchFactory, _bowlingFiguresCalculator, _playerOfTheMatchAward),
    new MatchesInTheFuture(_matchFactory, _teamFactory, _oversetFactory),
    new EveryMatchResultType(_matchFactory)
};
foreach (var provider in matchProviders)
{
    foreach (var match in provider.CreateMatches(testData)) { testData.Matches.Add(match); ... }
}
```

**When adding a new test scenario:** prefer extending an existing factory with optional
parameters, or adding a new provider, over adding another private method to
`SeedDataGenerator`. When you extract an existing private method out of `SeedDataGenerator`
into a factory or provider, move its unit test coverage with it (see
`Stoolball.UnitTests\Testing\Factories\OverFactoryTests.cs` for the pattern) and delete the
superseded test from `SeedDataGeneratorTests.cs`. Over time `SeedDataGenerator` should keep
shrinking down toward pure orchestration: calling factories/providers and assembling
`TestData`.

## 5. Wiring a new Factory or Provider into DI

Test projects resolve `SeedDataGenerator` and its collaborators from an `IServiceProvider`
built by `Stoolball.Testing\ServiceCollectionExtensions.cs`:

```csharp
public static IServiceCollection AddSeedDataGenerator(this IServiceCollection services)
{
    services.AddSingleton(new Randomiser(new Random()));
    services.AddSingleton<CompetitionFactory>();
    services.AddSingleton<SeasonFactory>();
    // ...one AddSingleton<T>() per Factory...

    services.AddSingleton(sp => new SeedDataGenerator(
        sp.GetRequiredService<Randomiser>(),
        sp.GetRequiredService<OverFactory>(),
        // ...resolves every SeedDataGenerator constructor argument...
        ));

    return services;
}
```

Consumers just do:

```csharp
using var serviceProvider = new ServiceCollection().AddSeedDataGenerator().BuildServiceProvider();
var randomSeedDataGenerator = serviceProvider.GetRequiredService<SeedDataGenerator>();
```

To add a brand-new Factory: add `services.AddSingleton<NewFactory>();` in
`AddSeedDataGenerator`, and if `SeedDataGenerator` needs it directly, add it to
`SeedDataGenerator`'s constructor and to the `sp.GetRequiredService<NewFactory>()` call
above. Providers are constructed inline where they're used (see §4) rather than registered
here, since each call site needs a different subset of factories.

## 6. Populate TestData properties/collections *after* generation finishes

`Stoolball.Testing\TestData.cs` is the bag exposed to every test: bulk `List<T>` collections
(`Teams`, `Competitions`, `Seasons`, `Matches`, ...) and singular "named example" properties
(`TeamWithFullDetails`, `CompetitionWithNoSeasons`, `SeasonWithMinimalDetails`, ...).

**Target pattern — select from the finished data.** Once every provider and factory has run,
rebuild the collection from everything that was actually generated, then pick the singular
examples from that finished collection:

```csharp
// SeedDataGenerator.cs — rebuilt from everything generated across the whole run
testData.Teams = poolOfTeamsWithPlayers.Select(x => x.team)
    .Union(teamsInMatches).Union(teamsInTournaments).Union(teamsInSeasons).Union(teamsAtMatchLocations)
    .Distinct(new TeamEqualityComparer()).ToList();

testData.TeamWithFullDetails = testData.Teams.First(x =>
    x.Club != null && x.MatchLocations.Any() && x.Seasons.Any() &&
    teamsInMatches.Select(t => t.TeamId).Contains(x.TeamId));
```

A small static `Find*` helper over the finished `TestData` is the same pattern, just
extracted for readability, e.g. `FindTeamWithMinimalDetails(testData, teamsInMatches)`.
This is robust to later code still mutating or filtering the same underlying list, since
the selection always runs last.

**Avoid — populating a public property mid-generation** (`testData.Competitions.Add(...)`
inside an early generation loop, before providers and later passes have run) — a downstream
step may still replace or filter that same collection, leaving the property holding stale or
incomplete data. Not all of `SeedDataGenerator` follows the target pattern yet; when you
touch a piece of it, prefer moving its property population to the end (a rebuild or a `Find*`
selector) rather than adding another incremental `.Add()` earlier in the method.

## 7. Where things live

```
Stoolball.Data.SqlServer.IntegrationTests\
  Fixtures\                 BaseSqlServerFixture, SqlServerTestDataFixture, TestDataIntegrationTestCollection
  <Aggregate>\              e.g. Clubs\SqlServerClubRepositoryTests.cs — one file while the repo is small
  <Aggregate>\<Repo>Tests\  one file per method + a shared <Repo>TestsBase.cs, once a repo has many methods
                            (see Matches\SqlServerMatchRepositoryTests\, Statistics\SqlServerPlayerRepositoryTests\)

Stoolball.Testing\          (references Bogus; kept out of production projects)
  Factories\                 one *Factory per entity type, each exposing CreateFaker(...)
  MatchDataProviders\        BaseMatchDataProvider + named edge-case scenarios
  CompetitionDataProviders\  BaseCompetitionDataProvider + scenarios
  PlayerDataProviders\       BasePlayerDataProvider + scenarios
  SchoolDataProviders\       BaseSchoolDataProvider + scenarios
  SeedDataGenerator.cs       shrinking orchestrator — avoid adding to it, extract from it
  ServiceCollectionExtensions.cs   AddSeedDataGenerator() DI registration
  TestData.cs                the generated-data bag exposed to tests
  Randomiser.cs, DateRangeGenerator.cs   unseeded randomness helpers (§3)
```

A new Factory or Provider goes in `Stoolball.Testing`, gets a unit test in
`Stoolball.UnitTests\Testing\Factories\` (or the matching `*DataProviders` folder), and is
registered in `ServiceCollectionExtensions.AddSeedDataGenerator` if `SeedDataGenerator` or a
test fixture needs to resolve it directly.
