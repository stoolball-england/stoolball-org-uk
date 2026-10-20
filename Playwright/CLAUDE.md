# Authoring Playwright tests in the Playwright folder

These are end-to-end tests that run in a browser against a running copy of the website,
by default at `https://localhost:44343`. See `docs/Testing.md` for how to start the site
and run the tests.

**Always run the affected tests after adding or changing a test here**, before reporting
the work as done. TypeScript compiling is not enough: the tests depend on the real
markup and data served by the site. If the site isn't running and can't be started, say
so explicitly rather than reporting success.

```pwsh
npx playwright test -c Playwright Club.spec.ts
```

Playwright runs TypeScript without type-checking it, so also check types using
`Playwright/tsconfig.json`:

```pwsh
npx -p typescript tsc -p Playwright
```

## Layout

- `playwright.config.ts` — base URL, browser and reporters. The base URL can be
  overridden with the `BASE_URL` environment variable.
- `tests/` — one `<PageOrFeature>.spec.ts` file per page or feature, named after the page,
  e.g. `Club.spec.ts`, `EditTeam.spec.ts`, `MatchesRss.spec.ts`.
- `support/assertions.ts` — shared assertions. Add new shared helpers here rather than
  duplicating logic across spec files.
- `.env` — secrets needed by tests, e.g. `CSV_EXPORT_API_KEY`. Ignored by git. Read
  secrets from `process.env`, never hard-code them.

## Writing tests

- Write tests in TypeScript and import `test` and `expect` from `@playwright/test`.
- Use relative URLs with `page.goto("/teams")` and `request.get("/teams")` so the base URL
  applies.
- Group tests with `test.describe` and navigate to the page in `test.beforeEach`. Use a
  nested `test.describe("When signed out", ...)` for pages that behave differently by
  authentication state.
- Every page test should have, at minimum, these two tests:

  ```ts
  import { test } from "@playwright/test";
  import { expectValidHtml, expectNoAccessibilityViolations } from "../support/assertions";

  test.describe("Club", () => {
    test.beforeEach(async ({ page }) => {
      await page.goto("/clubs/maresfield");
    });

    test("Validates", async ({ page }) => {
      await expectValidHtml(page);
    });

    test("Passes AXE", async ({ page }) => {
      await expectNoAccessibilityViolations(page);
    });
  });
  ```

- `expectValidHtml` runs html-validate over the rendered DOM. To turn off a rule for one
  page, pass html-validate config as the second argument, with a comment explaining why:
  `expectValidHtml(page, { rules: { "input-missing-label": "off" } }) // reason`. Only turn
  off rules site-wide in `support/assertions.ts` if they are wrong for every page.
- `expectNoAccessibilityViolations` runs axe-core. To skip rules for one page, pass an
  array of axe rule ids, each with a comment explaining why. To leave third-party markup we
  don't control (eg a YouTube iframe) out of the scan, pass CSS selectors as the third
  argument, with a comment. Don't skip rules to hide problems in markup we control — fix
  the markup instead.
- Pages that require authentication should check they are showing the sign-in form:
  `await expect(page.locator("body")).toContainText("Sign in");`
- For endpoints that don't return a page (feeds, CSV, headers, status codes) use the
  `request` fixture instead of `page`, and assert on `response.ok()`, `response.status()`
  or `response.headers()` (header names are lower case). `request.get` does not fail on
  error status codes, so always assert on the status.
- Use Playwright's web-first assertions (`await expect(locator).toHaveCount(...)`,
  `await expect(page).toHaveURL(...)`) which retry until they pass. Don't use
  `page.waitForTimeout` or fixed delays.
- Prefer user-facing locators (`getByRole`, `getByLabel`, `getByText`) for interactions.
  Use CSS selectors when targeting a specific id or BEM class is clearer, as in the search
  tests.
- Tests run in parallel and must be independent. They rely on data that already exists in
  the local database (e.g. the Maresfield club, the Mid Sussex Mixed League 2021 season),
  and must not change data that other tests depend on.
