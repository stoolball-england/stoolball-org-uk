# Ensure the main repository is on the master branch
$projectRoot = Resolve-Path "$PSScriptRoot\.."
Push-Location $projectRoot
git checkout master

# Go and get the latest changes from Umbraco Cloud and copy them into the Stoolball.Web
Push-Location $projectRoot\.UmbracoCloud
git pull
robocopy . ..\Stoolball.Web *.uda *.cshtml /S /XD .git
Pop-Location

# Trigger an update from remote to local by Umbraco deploy
if (!(Test-Path "./Stoolball.Web/data/deploy" -PathType Leaf)) {
    New-Item -Path "./Stoolball.Web/data" -Name "deploy" -ItemType "file"
}

Write-Host "THIS COULD LOSE YOUR WORK.

Review the git status below and commit changes to master if appropriate.
master will then be up-to-date with Umbraco Cloud." -ForegroundColor Red
git add .
git status

Pop-Location

