# Getting started

## Recommended software

The following software is recommended to work on this project:

- [Git for Windows](https://git-scm.com/download/win)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Visual Studio Community 2026 or better](https://visualstudio.microsoft.com/downloads/), which is the first version of Visual Studio to support .NET 10
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
3. Run `npm install` from a command line at the root of the repository
4. Open `Stoolball.sln` in Visual Studio
5. Right-click `compilerconfig.json` in the `Stoolball.Web` project and select `Web Compiler > Re-compile all files`.
6. Select the solution in Solution Explorer and press Alt+Enter (or right-click, Properties). In the Properties dialog select "Single startup project" and choose Stoolball.Web. Click OK.
7. Set up the [configuration](Configuration.md) for your environment.
8. Press Ctrl+F5 to run without debugging using IIS Express. This will open the Umbraco home page for a blank site. Click 'Open Umbraco' to open the login screen for the Umbraco back office. Login using your Umbraco.io account.
9. Go to the 'Settings' section and select 'uSync'. Click the dropdown next to the 'Import' button and select 'Import Content/Media'. This will load the standard content into the site. Optionally go to the 'Content' section, right-click the 'Content' node and select 'Reload nodes' to see the content.
10. Go to the 'Settings' section and select 'Languages'. Delete 'English (United States)'.

### Running the project in Visual Studio Code

Visual Studio is the best tool for editing the C# code of the project. However, Visual Studio Code is better for editing JavaScript, the AngularJS components of the Umbraco back office, and the Markdown documentation.

You need to compile the SCSS files with the Web Compiler extension in Visual Studio at least once. After that you can build and run the project from a terminal in Visual Studio Code, as described in [running the project from the command line](#running-the-project-from-the-command-line).

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
