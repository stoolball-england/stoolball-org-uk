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

## UI testing with Cypress

You can run [Cypress](https://www.cypress.io/) from the root of the repository using:

```pwsh
npx cypress open
```

To ensure all tests pass create a `cypress.env.json` file in the root of this repository with the following format. It will be ignored by git.

```json
{
    "CsvExportApiKey": "xxx"
}
```

Make sure `xxx` is the same as the API key specified in `Stoolball.Web/appsettings.Development.json`. See [configuration](Configuration.md).
