# Working in this repository

Some subdirectories have their own `CLAUDE.md` with rules specific to that project (for
example `Stoolball.Data.SqlServer.IntegrationTests/CLAUDE.md`, which requires running the
affected tests locally after any change there, before reporting the work as done — a
successful build is not enough).

When running the site locally, always run it in the `Development` environment by setting
`ASPNETCORE_ENVIRONMENT=Development`, and check that the output shows
`Hosting environment: Development`. `dotnet run --environment Development` is not enough. See
`docs/GettingStarted.md`.

## Playwright tests for pages

The end-to-end tests in `Playwright/` cover every page on the site. See `Playwright/CLAUDE.md`
for how to write and run them.

- **New pages:** every new page needs a spec in `Playwright/tests/` with at least the basic
  tests: `Validates` (`expectValidHtml`), `Passes AXE` (`expectNoAccessibilityViolations`)
  and, if the page requires authentication, `Requires authentication` (checks for the sign-in
  form when signed out). A page is a new controller and view, a new route in
  `StoolballRouteParser.cs` that renders a different view, or a new Umbraco template. A new
  URL that reuses an existing view and already has a spec doesn't need another one.
- **Changes to a page's UI:** a change to the markup, behaviour or content of a page (views,
  partials, view models, controllers, CSS or JavaScript) should come with a test that covers
  the change, added to that page's spec. Extend the existing spec rather than adding a new
  file.
- **Run the tests:** after any change that affects a page's UI, run the Playwright specs for
  the affected pages against the site running locally, before reporting the work as done. A
  change to a shared partial, layout, CSS or JavaScript file affects every page that uses it,
  so run those specs too, or the whole suite if it is used widely. A successful build or
  typecheck is not enough. If the site isn't running and can't be started, say so explicitly
  rather than reporting success.

Before editing files in a directory for the first time in a session, check for a
`CLAUDE.md` in that directory and in each parent directory up to the repository root
(for example, editing a file in `Stoolball.Testing/Factories/` means checking both
`Stoolball.Testing/Factories/` and `Stoolball.Testing/`), and follow them. Its instructions are not always surfaced
automatically just because this root file has been read.
