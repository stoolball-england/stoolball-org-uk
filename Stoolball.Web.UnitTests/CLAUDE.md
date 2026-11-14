# Authoring unit tests in Stoolball.Web.UnitTests

## Mocking the ranked statistics data sources

The ranked statistics methods on `IBestPerformanceInAMatchStatisticsDataSource`,
`IBestPlayerTotalStatisticsDataSource` and `IBestPlayerAverageStatisticsDataSource` (for
example `ReadPlayerInnings`, `ReadMostRunsScored`, `ReadBestBattingAverage`) return
`StatisticsResults<T>`, not `IEnumerable<StatisticsResult<T>>`.

Moq returns an empty `IEnumerable<T>` from a method that hasn't been set up, but it returns
`null` for `StatisticsResults<T>`. A controller that calls `.ToList()` on the result then
throws `ArgumentNullException`, even in tests that don't care about the statistics.

To avoid this, create the mock with `WithEmptyResults()` from
`StatisticsDataSourceMockExtensions.cs`. It sets up every ranked method to return empty
results:

```csharp
private readonly Mock<IBestPerformanceInAMatchStatisticsDataSource> _bestPerformanceDataSource = new Mock<IBestPerformanceInAMatchStatisticsDataSource>().WithEmptyResults();
private readonly Mock<IBestPlayerTotalStatisticsDataSource> _bestTotalDataSource = new Mock<IBestPlayerTotalStatisticsDataSource>().WithEmptyResults();
```

Call it when you create the mock, as above, not later in a test. Moq uses the most recent
matching setup, so calling it after a test's own setups would replace them with empty
results.

To return results from a test, wrap them in `StatisticsResults<T>`:

```csharp
_bestPerformanceDataSource.Setup(x => x.ReadPlayerInnings(It.IsAny<StatisticsFilter>(), StatisticsSortOrder.BestFirst))
    .ReturnsAsync(new StatisticsResults<PlayerInnings>(innings));
```

There is no `WithEmptyResults()` for `IBestPlayerAverageStatisticsDataSource` yet, because no
test has needed one. If you add one, put it in `StatisticsDataSourceMockExtensions.cs`
alongside the others.
