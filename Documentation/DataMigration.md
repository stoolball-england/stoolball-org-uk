# Migrating data

Data needs to be migrated from the old Stoolball England website to this one in a repeatable way, so that the migration can be tested and then run for real when the time comes to replace the existing website.

Data migration is integrated with the Umbraco back office using [AngularJS](https://angularjs.org/). It works as follows:

1. In most cases no API exists on the existing Stoolball England website, so one must be built which exposes the data as JSON. In cases where the API exposes personal data, such as migrating member accounts, this API must be protected by requiring an API key.
2. A `Stoolball.DataMigration` Umbraco plugin can be found in the `Stoolball.Web\App_Plugins` folder. It adds a new 'Data migration' dashboard to the Settings section of Umbraco. Buttons to trigger specific data migration tasks are added to `DataMigrationDashboard.html` and wired up in `dataMigrationDashboardController.js`.
3. Each data migration task should open a dialog, for which an HTML file should be placed in the `dialogs` subfolder. The controllers for these are again wired up in `dataMigrationDashboardController.js`.
4. Data migration tasks will normally need to talk to a .NET API on the Umbraco side. API controllers created for data migration should always inherit from `UmbracoAuthorizedJsonController`.
5. Calls from JavaScript to the .NET API should be added to `stoolballResource.js`, which is injected as a dependency into the JavaScript controller for each dialog in `dataMigrationDashboardController.js`.

[Visual Studio Code](https://code.visualstudio.com/) with the [Jest](https://marketplace.visualstudio.com/items?itemName=Orta.vscode-jest) extension is recommended for editing JavaScript as Jest tests can be added and run much more easily than in Visual Studio.
