# Ensure the main repository is on the develop branch
# TODO: Assumes develop exists at this point
$projectRoot = Resolve-Path "$PSScriptRoot\.."
Push-Location $projectRoot
git checkout develop

# Go and get the latest changes from Umbraco Cloud and copy them into the Stoolball.Web
Push-Location $projectRoot\.UmbracoCloud
git pull
robocopy . ..\Stoolball.Web *.uda *.cshtml /S /XD .git
Pop-Location

# Commit the new changes to Stoolball.Web on develop. Develop is now up-to-date with Umbraco Cloud.
git add .
git commit -m "Merging changes from remote into develop using Pull-UmbracoCloud.ps1"

# Trigger an update from remote to local by Umbraco deploy
if (!(Test-Path "./Stoolball.Web/data/deploy" -PathType Leaf)) {
    New-Item -Path "./Stoolball.Web/data" -Name "deploy" -ItemType "file"
}
Pop-Location

