# Getting started

## Recommended software

The following software is recommended to work on this project:

- [Git for Windows](https://git-scm.com/download/win)
- [NuGet command line](https://www.nuget.org/downloads)
- [Visual Studio Community 2019 or better](https://visualstudio.microsoft.com/downloads/)
- [Web Compiler extension](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.WebCompiler) for Visual Studio
- [Razor Generator extension](https://marketplace.visualstudio.com/items?itemName=DavidEbbo.RazorGenerator) for Visual Studio
- [Visual Studio Code](https://code.visualstudio.com/)
- [Jest](https://marketplace.visualstudio.com/items?itemName=Orta.vscode-jest) and [Prettier](https://marketplace.visualstudio.com/items?itemName=esbenp.prettier-vscode) for Visual Studio Code
- [SQL Server Express LocalDB or better](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver15)
- [Node.js LTS including npm](https://nodejs.org/en/)
- [Papercut SMTP development server](https://github.com/ChangemakerStudios/Papercut)

## Clone and run the project

Follow these steps to clone and run the project. You will need the git repository URL and authentication details of an Umbraco Cloud project. If you don't have access to the Umbraco Cloud project for this website, you can use an Umbraco Cloud trial account.

1. Clone this repository
2. Clone the Umbraco Cloud git repository into a folder called `.UmbracoCloud` inside the root folder of this repository
3. Run `npm install` from a command line at the root of the repository
4. Open `Stoolball.sln` in Visual Studio
5. Right-click `compiler-config.json` in the `Stoolball.Web` project and select `Web Compiler > Re-compile all files`.
6. Select the solution in Solution Explorer and press Alt+Enter (or right-click, Properties). In the Properties dialog select "Single startup project" and choose Stoolball.Web. Click OK.
8. Set up the [configuration](Configuration.md) for your environment.
7. Press Ctrl+F5 to run without debugging using IIS Express. This will open the Umbraco home page for a blank site. Click 'Open Umbraco' to open the login screen for the Umbraco back office. Login using your Umbraco.io account.
8. Go to the 'Settings' section and select 'uSync'. Click the dropdown next to the 'Import' button and select 'Import Content/Media'. This will load the standard content into the site. Optionally go to the 'Content' section, right-click the 'Content' node and select 'Reload nodes' to see the content.
9. Go to the 'Settings' section and select 'Languages'. Delete 'English (United States)'.

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

### Running the project from the command line

Local development should always run the site in the `Development` environment, whether you, a script or an AI assistant starts it. In any other environment the site does not load `appsettings.Development.json`, so it uses a different database and settings. Running it like that can change local files such as the Umbraco Deploy `.uda` files and ModelsBuilder models to match the wrong database.

Visual Studio's IIS Express profile already sets `ASPNETCORE_ENVIRONMENT` to `Development`. From the command line, set the environment variable yourself, then run the site from the root of the repository:

```pwsh
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project Stoolball.Web --no-launch-profile --urls "https://localhost:44343"
```

Don't rely on `dotnet run --environment Development`, which does not set the environment in all versions of the .NET SDK. Check that the console output shows `Hosting environment: Development`.

## Managing redirects

Redirects are managed using the [Skybrud.Umbraco.Redirects](https://github.com/skybrud/Skybrud.Umbraco.Redirects) package, which appears as a dashboard named 'Redirects' in the Content section of the Umbraco back office.
