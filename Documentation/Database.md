# Working with stoolball data

Stoolball data such as leagues, teams and matches is held in custom tables in the Umbraco database.

## Inspecting the Umbraco database

To check the content of the custom tables, stop IIS Express if it's running and use the Server Explorer window in Visual Studio to create a new 'Data Connection' using 'Microsoft SQL Server database file' as the 'Data source'. Connect to `Stoolball.Web\App_Data\Umbraco.mdf`. When you're finished click 'Close Connection'.

### If Umbraco fails to boot

If you run Umbraco after inspecting the database in Visual Studio you may get an error saying Umbraco failed to boot. If so the database is probably still locked by SQL Server. Open a PowerShell prompt
in the root of this repository and run `.\Scripts\Stop-SqlServer.ps1` to kill the lock, then refresh the browser.

## Creating and upgrading the database

The schema for the custom tables is set up in the `Stoolball.Umbraco.Data.Migrations` namespace and is based on [Creating a custom database table](https://our.umbraco.com/Documentation/Extending/Database/) in the Umbraco documentation. Classes in this project are re-run each time Umbraco starts and they **must** remain the same as when they first ran.

**Do not change any file in this namespace except as shown below.**

To update the stoolball data schema, such as to add, remove or change tables or columns, add a new migration that runs after those already in this namespace. You can do this in three stages:

1. If you're adding a new table, create a new constant for the table name in `Constants.cs`. Then copy any file called `*TableInitialSchema.cs` and update the copy to your needs, using the new constant you defined to set the table name. If you're updating or removing an existing table you can skip this step.
2. Copy any file called `*AddTable.cs` and update the copy to your needs. If you're changing a table rather than adding one then other methods are available. For example, to add a column:

   ```csharp
   if (ColumnExists(Constants.Tables.Example, "ExampleColumn") == false) {

       Create.Column("ExampleColumn")
       .OnTable(Constants.Tables.Example)
       .AsInt32().Do();
   }
   ```

3. Add your new migration after those already configured in `StoolballMigrations.cs` and restart Umbraco.

## Auditing changes

All create, update and delete actions on stoolball data should be audited. This adds a record to a `StoolballAudit` table with details of who did what, when, and the resulting serialised object.

To audit a change inject `IAuditRepository` into your class and call `IAuditRepository.CreateAudit()`:

```csharp
async Task CreateSomeThing(Thing thingToAudit)
{
    // ... create the thing here

    // now audit the creation of the thing
    var loggedInUmbracoMember = Members.GetCurrentMember();

    await _auditRepository.CreateAudit(new AuditRecord
    {
        Action = AuditAction.Create,
        MemberKey = loggedInUmbracoMember.Key, // or null for an automated process
        ActorName = loggedInUmbracoMember.Name, // or nameof(SomeClass) for an automated process
        EntityUri = thingToAudit.EntityUri,
        State = JsonConvert.SerializeObject(thingToAudit),
        AuditDate = DateTimeOffset.UtcNow
    }).ConfigureAwait(false);
}
```
