# Testing

## .NET testing with xUnit.net

Run xUnit tests using Test > Run All Tests in Visual Studio.

## JavaScript testing with Jest

[Visual Studio Code](https://code.visualstudio.com/) with the [Jest](https://marketplace.visualstudio.com/items?itemName=Orta.vscode-jest) and [Prettier](https://marketplace.visualstudio.com/items?itemName=esbenp.prettier-vscode) extensions is recommended for editing JavaScript, because Visual Studio doesn't have good support for Jest in its test runner.

Create or update a file called `.vscode/settings.json` and add the following configuration for Jest:

```json
{
  "jest.rootPath": "Stoolball.Web"
}
```

Create a file called `.vscode/launch.json` with the following configuration for Jest:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "type": "node",
      "name": "vscode-jest-tests",
      "request": "launch",
      "args": ["--runInBand"],
      "cwd": "${workspaceFolder}",
      "console": "integratedTerminal",
      "internalConsoleOptions": "neverOpen",
      "disableOptimisticBPs": true,
      "program": "${workspaceFolder}/node_modules/jest/bin/jest"
    }
  ]
}
```

Jest test files should be named `*.test.js`.

To get started, open a PowerShell prompt at the root of this repository and run:

```pwsh
npm install
npx jest
```

## UI testing with Playwright

End-to-end tests use [Playwright](https://playwright.dev/) and are written in TypeScript. They live in the `Playwright` folder, and check that pages render valid HTML, pass [axe](https://github.com/dequelabs/axe-core) accessibility checks and behave as expected. See `Playwright/CLAUDE.md` for how tests should be written.

The tests run against a copy of the website that must already be running at `https://localhost:44343`. Start it from Visual Studio using the IIS Express profile, or from the command line using:

```pwsh
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project Stoolball.Web --no-launch-profile --urls "https://localhost:44343"
```

Always run the site in the `Development` environment. See [running the project from the command line](GettingStarted.md#running-the-project-from-the-command-line).

To test a different URL, set the `BASE_URL` environment variable.

The first time, install the npm packages and the browser used by Playwright from the root of the repository:

```pwsh
npm install
npx playwright install chromium
```

To ensure all tests pass create a `Playwright/.env` file with the following format. It will be ignored by git.

```
CSV_EXPORT_API_KEY=xxx
```

Make sure `xxx` is the same as the API key specified in `Stoolball.Web/appsettings.Development.json`. See [configuration](Configuration.md).

Run all the tests from the root of the repository using:

```pwsh
npm run test:e2e
```

Other useful commands:

```pwsh
npx playwright test -c Playwright Club.spec.ts   # run one spec file
npx playwright test -c Playwright -g "Passes AXE" # run tests whose title matches
npm run test:e2e:ui                               # open the interactive UI mode
npx playwright show-report Playwright/playwright-report # view the HTML report of the last run
```

Tests tagged `@Production` only read data, so they are safe to run against the live site. Run them using:

```pwsh
$env:BASE_URL = "https://www.stoolball.org.uk"
npx playwright test -c Playwright --grep "@Production" --workers=2
```

For the CSV export test to pass against a site that is not `localhost`, add the production API key to `Playwright/.env` as `PRODUCTION_CSV_EXPORT_API_KEY`:

```
PRODUCTION_CSV_EXPORT_API_KEY=xxx
```

`Scripts/Push-UmbracoCloud.ps1` runs all the tests against a local copy of the site before deploying, then waits for the new version to appear on production and runs the `@Production` tests with 2 workers. The production run writes its report to `Playwright/playwright-report-production` so it does not replace the report from the local run. Use `-SkipPlaywright` to skip both runs.

In Visual Studio Code, the [Playwright Test for VSCode](https://marketplace.visualstudio.com/items?itemName=ms-playwright.playwright) extension lets you run and debug individual tests from the editor.
