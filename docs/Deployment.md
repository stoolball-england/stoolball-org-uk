# Deployment

## Compiling views

To detect compilation errors in Razor files early, and to avoid the delay on a production deployment that comes with view compilation, the [RazorGenerator.Mvc](https://github.com/RazorGenerator/RazorGenerator) NuGet package is used to pre-compile the views into .cs files which are included in Stoolball.Web.dll. This requires the [Razor Generator extension](https://marketplace.visualstudio.com/items?itemName=DavidEbbo.RazorGenerator) for Visual Studio to be installed.

If a new view is added then the 'Custom Tool' option on its properties should be set to `RazorGenerator`. When the .cshtml file is saved a `{viewname}.generated.cs` file should appear nested below it in the Visual Studio Solution Explorer window.

Normally pre-compiling views means that the .cshtml files would not need to be deployed to the production site. However, Umbraco Deploy checks for their existence and will not work unless they're there, so they must still be deployed even though they're not used at runtime.

Razor Generator is used in favour of the `<MvcBuildViews>true</MvcBuildViews>` option which can be added to a .csproj file, because that only checks the views for errors at build time. It doesn't create pre-compiled code that can be deployed. It's used in favour of `aspnet_compiler.exe` because that works in a publish profile triggered from within Visual Studio, but it's very difficult to automate as part of a deployment script.
