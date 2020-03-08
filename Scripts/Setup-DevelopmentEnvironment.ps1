param(
    [Parameter(Mandatory = $true)]
    $UmbracoCloudRepoUrl
)
$projectRoot = Resolve-Path "$PSScriptRoot\.."
Push-Location $projectRoot

# Restore NuGet packages and files from those packages.
nuget restore Stoolball.sln
robocopy .\packages\UmbracoCms.8.5.4\Content\Views\Partials .\Stoolball.Web\Views\Partials /s
robocopy .\packages\UmbracoForms.8.3.1\Content\ .\Stoolball.Web /s
robocopy .\packages\bootstrap.sass.4.4.1\contentFiles .\Stoolball.Web\ /s
robocopy .\packages\jQuery.3.0.0\Content .\Stoolball.Web\ /s
robocopy .\packages\popper.js.1.16.0\content .\Stoolball.Web\ /s

# Clone the Umbraco Cloud repo. This may prompt for authentication
git clone $UmbracoCloudRepoUrl .UmbracoCloud

# Copy files from the Umbraco Cloud repo to the Stoolball.Web project
# (Mostly secrets and Umbraco Deploy, which is not available on NuGet yet.)
robocopy .\.UmbracoCloud\App_Plugins\Deploy .\Stoolball.Web\App_Plugins\Deploy /s
robocopy .\.UmbracoCloud\App_Plugins\UmbracoLicenses .\Stoolball.Web\App_Plugins\UmbracoLicenses /s
robocopy .\.UmbracoCloud\Config .\Stoolball.Web\config /IF UmbracoDeploy.*
robocopy .\.UmbracoCloud\Config .\Stoolball.Web\config /IF Secret-*.xdt.config
robocopy .\.UmbracoCloud\umbraco\Config\Lang .\Stoolball.Web\umbraco\Config\Lang /s
robocopy .\.UmbracoCloud\bin .\Stoolball.Web\bin /IF Umbraco.Deploy.*
robocopy .\.UmbracoCloud\data\backoffice .\Stoolball.Web\data\backoffice /s
robocopy .\.UmbracoCloud .\Stoolball.Web /IF Web.config
robocopy .\.UmbracoCloud .\Stoolball.Web /IF Secret-*.xdt.config

# Trigger an update from remote to local by Umbraco deploy
if (!(Test-Path "./Stoolball.Web/data/deploy" -PathType Leaf)) {
    New-Item -Path "./Stoolball.Web/data" -Name "deploy" -ItemType "file"
}

Pop-Location