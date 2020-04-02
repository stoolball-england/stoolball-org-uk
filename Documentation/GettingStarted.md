# Getting started

## Recommended software

The following software is recommended to work on this project:

- [Git for Windows](https://git-scm.com/download/win)
- [NuGet command line](https://www.nuget.org/downloads)
- [Visual Studio Community 2019 or better](https://visualstudio.microsoft.com/downloads/)
- [Web Compiler extension](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.WebCompiler) for Visual Studio
- [Visual Studio Code](https://code.visualstudio.com/)
- [Jest](https://marketplace.visualstudio.com/items?itemName=Orta.vscode-jest) and [Prettier](https://marketplace.visualstudio.com/items?itemName=esbenp.prettier-vscode) for Visual Studio Code
- [SQL Server Express LocalDB or better](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver15)
- [Node.js LTS including npm](https://nodejs.org/en/)
- [Papercut SMTP development server](https://github.com/ChangemakerStudios/Papercut)

## Clone and run the project

Follow these steps to clone and run the project. You will need the git repository URL and authentication details of an Umbraco Cloud project. If you don't have access to the Umbraco Cloud project for this website, you can use an Umbraco Cloud trial account.

1. Clone this repository
2. Clone the Umbraco Cloud git repository into a folder called `.UmbracoCloud` inside the root folder of this repository
3. Open `Stoolball.sln` in Visual Studio
4. Press Ctrl+F5 to run without debugging using IIS Express

This will open the login screen for the Umbraco back office. Login using your Umbraco.io account.

### Running the project in Visual Studio Code

Visual Studio is the best tool for editing the .NET Framework code of the project. However, Visual Studio Code is better for editing JavaScript, the AngularJS components of the Umbraco back office, and the Markdown documentation.

You need to run Visual Studio (or the equivalent MSBuild command) at least once to build the code, but after that running the project from Visual Studio Code may be useful when working on client-side changes. To enable this install the [IIS Express extension for Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=warren-buckley.iis-express), and create an `iisexpress.json` file in the `.vscode` folder at the root of the repository. Replace `c:\\src` with the path to the repository on your machine:

```json
{
  "port": 44343,
  "path": "c:\\src\\Stoolball.Web",
  "clr": "v4.0",
  "protocol": "https",
  "url": "/umbraco"
}
```

You can now use `View > Command Palette > IIS Express: Start Website` to open the Umbraco back office.

## Starting a new feature or fix

Run `.\Scripts\Pull-UmbracoCloud.ps1` to pull changes from Umbraco Cloud into `master` before starting new work.

Always work on a feature branch named `feature/xxx` or `fix/xxx` based off of `master`.

## Merge into `master` and push to Umbraco Cloud

Run `.\Scripts\Pull-UmbracoCloud.ps1` again to pull changes from Umbraco Cloud before merging into `master`.

The pull from Umbraco Cloud always gets committed to `master`, so the changes should be there before you merge yours in. This should avoid the remote changes overwriting yours.

You can merge `master` into your `feature/xxx` or `fix/xxx` branch if required.

Once your branch is merged into `master` run `.\Scripts\Push-UmbracoCloud.ps1` to push your changes. This should keep the remote up-to-date and minimise conflicts.

## Managing secrets in config files

`*.config` files sometimes need to contain secrets. You can use XDT transforms to add these to the relevant config file at deploy time using the [Umbraco Cloud per-environment naming convention](https://our.umbraco.com/documentation/Umbraco-Cloud/Set-Up/Config-Transforms/).

Begin the filename with `Secret-` (for example `Secret-MyPassword.{config file name}.{environment}.xdt.config`) and commit it to the `.UmbracoCloud` repository, which is private. It will be copied into this application when you build.

## Managing redirects

Redirects are managed using the [Skybrud.Umbraco.Redirects](https://github.com/skybrud/Skybrud.Umbraco.Redirects) package, which appears as a dashboard named 'Redirects' in the Content section of the Umbraco back office.
