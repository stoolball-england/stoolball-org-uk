# Getting started

The following software is recommended to work on this project:

- [Git for Windows](https://git-scm.com/download/win)
- [NuGet command line](https://www.nuget.org/downloads)
- [Visual Studio Community 2019 or better](https://visualstudio.microsoft.com/downloads/)
- [Web Compiler extension for Visual Studio](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.WebCompiler)
- [Markdown Editor extension for Visual Studio](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.MarkdownEditor)
- [SQL Server Express LocalDB or better](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver15)
- [Node.js LTS including npm](https://nodejs.org/en/)
- [Papercut SMTP development server](https://github.com/ChangemakerStudios/Papercut)

Clone this project, then open a PowerShell prompt in the cloned folder, and then run the following command. You will need the git repository URL of an Umbraco Cloud project and authentication details to connect to it. This can be an Umbraco Cloud trial account.

```pwsh
.\Scripts\Setup-DevelopmentEnvironment.ps1 -UmbracoCloudRepoUrl <url>
```

Open `Stoolball.sln` in Visual Studio. Select the `Stoolball.Web` project and press Ctrl+F5 to run without debugging using IIS Express.

Click "Open Umbraco" to navigate to the [Umbraco back office](https://localhost:44343/umbraco). Login using your Umbraco.io account.

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

Begin the filename with `Secret-` (for example `Secret-MyPassword.{config file name}.{environment}.xdt.config`) and commit it to the `.UmbracoCloud` repository, which is private. It will be copied into this application when you run the scripts above.
