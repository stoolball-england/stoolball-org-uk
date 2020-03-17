# Testing

## .NET testing with xUnit.net

Run xUnit tests using Test > Run All Tests in Visual Studio.

## JavaScript testing with Jest

[Visual Studio Code](https://code.visualstudio.com/) with the [Jest](https://marketplace.visualstudio.com/items?itemName=Orta.vscode-jest) and [Prettier](https://marketplace.visualstudio.com/items?itemName=esbenp.prettier-vscode) extensions is recommended for editing JavaScript, because Visual Studio doesn't have good support for Jest in its test runner.

Jest test files should be named `*.test.js`.

To get started, open a PowerShell prompt at the root of this repository and run:

```pwsh
npm install
jest
```

If Windows can't find `jest`, add the `node_modules\.bin` folder to your `PATH` environment variable. For example if you work in `c:\src`, add `c:\src\node_modules\.bin` to your `PATH`.

## UI testing with Cypress

You can run [Cypress](https://www.cypress.io/) from the root of the repository using:

```pwsh
npx cypress open
```
