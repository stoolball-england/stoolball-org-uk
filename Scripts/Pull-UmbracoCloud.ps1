# Go and get the latest changes from Umbraco Cloud 
$projectRoot = Resolve-Path "$PSScriptRoot\.."
Push-Location $projectRoot\.UmbracoCloud
git pull origin master
Pop-Location