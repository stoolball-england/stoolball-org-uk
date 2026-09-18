# Working in this repository

Some subdirectories have their own `CLAUDE.md` with rules specific to that project (for
example `Stoolball.Data.SqlServer.IntegrationTests/CLAUDE.md`, which requires running the
affected tests locally after any change there, before reporting the work as done — a
successful build is not enough).

Before editing files in a directory for the first time in a session, check for a
`CLAUDE.md` in that directory and in each parent directory up to the repository root
(for example, editing a file in `Stoolball.Testing/Factories/` means checking both
`Stoolball.Testing/Factories/` and `Stoolball.Testing/`), and follow them. Its instructions are not always surfaced
automatically just because this root file has been read.
