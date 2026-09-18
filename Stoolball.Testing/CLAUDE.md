# Working in Stoolball.Testing

This project generates the seed data used by the integration tests. Before changing anything
here, read `Stoolball.Data.SqlServer.IntegrationTests/CLAUDE.md`. It explains the Factory and
Provider pattern, how to wire new ones into DI, and how `TestData` must be built.

**Always run the integration tests (`dotnet test Stoolball.Data.SqlServer.IntegrationTests`)
after any change to test data in this project, before reporting the work as done.** A
successful build is not enough. If the tests can't be run (for example no database is
reachable), say so explicitly rather than reporting success.

Also run `Stoolball.Testing.UnitTests`, and add or update a unit test there for any new or
changed Factory or Provider.
