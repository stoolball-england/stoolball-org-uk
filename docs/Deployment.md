# Deployment

The site is deployed to Umbraco Cloud using `Scripts/Push-UmbracoCloud.ps1`. The script builds and tests the code, then copies the source of each project into the `.UmbracoCloud` repository and pushes it. Umbraco Cloud builds the site from that source.

## Compiling views

Razor views are compiled when the project is built, because `RazorCompileOnBuild` and `RazorCompileOnPublish` are set in `Stoolball.Web.csproj`. This finds errors in views early and avoids compiling them on production. ModelsBuilder runs in `SourceCodeAuto` mode, so the models are part of the compiled code.

Umbraco Deploy checks that the `.cshtml` files exist, so they are still deployed even though the compiled views are used at runtime.
