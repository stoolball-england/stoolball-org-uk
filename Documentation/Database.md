# Working with stoolball data

Stoolball data such as leagues, teams and matches is held in custom tables in the Umbraco database.

## Inspecting the Umbraco database

To check the content of the custom tables, stop IIS Express if it's running and use the Server Explorer window in Visual Studio to create a new 'Data Connection' using 'Microsoft SQL Server database file' as the 'Data source'. Connect to `Stoolball.Web\App_Data\Umbraco.mdf`. When you're finished click 'Close Connection'.

### If Umbraco fails to boot

If you run Umbraco after inspecting the database in Visual Studio you may get an error saying Umbraco failed to boot because "A connection string is configured but Umbraco could not connect to the database". If so the database is probably still locked by SQL Server. Open a PowerShell prompt
in the root of this repository and run `.\Scripts\Stop-SqlServer.ps1` to kill the lock, then refresh the browser.

## Creating and upgrading the database

The schema for the custom tables is set up in the `Stoolball.Data.UmbracoMigrations` namespace and is based on [Creating a custom database table](https://our.umbraco.com/Documentation/Extending/Database/) in the Umbraco documentation. Classes in this project are re-run each time Umbraco starts and they **must** remain the same as when they first ran.

**Do not change any file in this namespace except as shown below.**

To update the stoolball data schema, such as to add, remove or change tables or columns, add a new migration that runs after those already in this namespace. You can do this in three stages:

1. If you're adding a new table, create a new constant for the table name in `Stoolball.Data.SqlServer.Constants.cs`. Then copy any file called `*TableInitialSchema.cs` and update the copy to your needs, using the new constant you defined to set the table name. If you're updating or removing an existing table you can skip this step.
2. Copy any file called `*AddTable.cs` and update the copy to your needs. If you're changing a table rather than adding one then other methods are available. For example, to add a column:

   ```csharp
   if (ColumnExists(Tables.Example, "ExampleColumn") == false) {

       Create.Column("ExampleColumn")
       .OnTable(Tables.Example)
       .AsInt32().Do();
   }
   ```

3. Add your new migration after those already configured in `StoolballMigrations.cs` and restart Umbraco.

## Selecting data

Selecting data uses [Dapper](https://github.com/StackExchange/Dapper) rather than Umbraco's own method of connecting to its database, because [NPoco](https://discoverdot.net/projects/npoco) has only limited support for complex joins and asynchronous queries and cannot select into a `DateTimeOffset` type, which is used for all date properties. Table names are referenced using constants from the `Stoolball.Data.SqlServer.Constants` namespace.

Inject `IDatabaseConnectionFactory` into your class and use it as follows. In this example `connection.QueryAsync<Club, Team, Club>` indicates `Club` and `Team` are joined to return a `Club`:

```csharp
var clubs = await connection.QueryAsync<Club, Team, Club>(
    $@"SELECT c.ClubId, c.ClubRoute,
        t.TeamId, t.TeamRoute
        FROM {Tables.Club} AS c
        INNER JOIN {Tables.Team} AS t ON c.ClubId = t.ClubId
        WHERE LOWER(c.ClubId) = @ClubId",
    (club, team) =>
    {
        club.Teams.Add(team);
        return club;
    },
    new { ClubId = "some-guid-id" },
    splitOn: "TeamId"
)
.ConfigureAwait(false);
```

When Dapper selects into a `DateTimeOffset`, by default it assumes the timezone of the current culture. All dates are stored in the database as UTC, so assuming another timezone causes the time to be incorrect. `DapperComposer` wires up `DapperDateTimeHandler`, which tells Dapper to interpret any dates coming from the database as UTC.

## Creating, updating and deleting data

Creating, updating and deleting data is the same as selecting, except that it uses a transaction.

```csharp
using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
{
    connection.Open();
    using (var transaction = connection.BeginTransaction())
    {
        await connection.ExecuteAsync(
            $@"INSERT INTO {Tables.Example}
               (ExampleId, ExampleField)
               VALUES
               (@ExampleId, @ExampleField)",
            new {
                ExampleId = Guid.NewGuid(),
                ExampleField = "some value"
            },
            transaction).ConfigureAwait(false);

        transaction.Commit();
    }
}
```

### Auditing changes

All create, update and delete actions on stoolball data should be audited. This adds a record to a `StoolballAudit` table with details of who did what, when, and the resulting serialised object.

To audit a change inject `IAuditRepository` into your class and call `IAuditRepository.CreateAudit()`:

```csharp
async Task CreateSomeThing(Thing thingToAudit, IDbTransaction transaction)
{
    // ... create the thing here

    // now audit the creation of the thing (production code would pass in the member name)
    var loggedInUmbracoMember = Members.GetCurrentMember();

    await _auditRepository.CreateAudit(new AuditRecord
    {
        Action = AuditAction.Create,
        MemberKey = loggedInUmbracoMember.Key, // or null for an automated process
        ActorName = loggedInUmbracoMember.Name, // or nameof(SomeClass) for an automated process
        EntityUri = thingToAudit.EntityUri,
        State = JsonConvert.SerializeObject(thingToAudit),
        RedactedState = JsonConvert.SerialiseObject(CreateRedactedCopy(thingToAudit))
        AuditDate = DateTimeOffset.UtcNow
    }, transaction).ConfigureAwait(false);
}

private Thing CreateRedactedCopy(Thing thing)
{
    /// ... copy and redact personal info here
}
```
