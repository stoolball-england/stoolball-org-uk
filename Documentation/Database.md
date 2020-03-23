# Working with stoolball data

Stoolball data such as leagues, teams and matches is held in custom tables in the Umbraco database.

The schema for these tables is set up in the `Stoolball.Umbraco.Migrations` project and is based on [Creating a custom database table](https://our.umbraco.com/Documentation/Extending/Database/) in the Umbraco documentation. Classes in this project are re-run each time Umbraco starts and they **must** remain the same as when they first ran.

**Do not change any file in this project except as shown below.**

To update the stoolball data schema, such as to add, remove or change tables or columns, add a new migration that runs after those already in this project. You can do this in three stages:

1. If you're adding a new table, create a new constant for the table name in `Constants.cs`. Then copy any file called `*TableSchema.cs` and update the copy to your needs, using the new constant you defined to set the table name. If you're updating or removing an existing table you can skip this step.
2. Copy any file called `*AddTable.cs` and update the copy to your needs. If you're changing a table rather than adding one then other methods are available. For example, to add a column:

   ```csharp
   if (ColumnExists(Constants.Tables.Example, "ExampleColumn") == false) {

       Create.Column("ExampleColumn")
       .OnTable(Constants.Tables.Example)
       .AsInt32().Do();
   }
   ```

3. Add your new migration after those already configured in `StoolballMigrations.cs` and restart Umbraco.

## Inspecting the Umbraco database

To check that your migration worked, stop IIS Express if it's still running and use the Server Explorer window in Visual Studio to create a new 'Data Connection' using 'Microsoft SQL Server database file' as the 'Data source'. Connect to `Stoolball.Web\App_Data\Umbraco.mdf`.

## If Umbraco fails to boot

When you're finished click 'Close Connection'. If you then run Umbraco you may get an error saying Umbraco failed to boot. If so the database is probably still locked by SQL Server. Open a PowerShell prompt
in the root of this repository and run `.\Scripts\Stop-SqlServer.ps1` to kill the lock.
