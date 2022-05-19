$projectRoot = Resolve-Path "$PSScriptRoot\.."
$cloudRoot = Resolve-Path "$projectRoot\.UmbracoCloud"

$foldersToCopy = "Stoolball", "Stoolball.Data.SqlServer", "Stoolball.Data.Cache", "Stoolball.Data.UmbracoMigrations", "Stoolball.Web"

# Get the path to the Umbraco project from the .umbraco Umbraco Cloud config file
$projectConfig = Get-Content $projectRoot/.umbraco
$configLine = $projectConfig | Where-Object { $_.StartsWith("csproj") } | Select-Object -First 1
if ($configLine -match 'csproj = "(?<csproj>[\w.-]+)"') {
    $umbracoProjectFile = $Matches.csproj
}


# Pull updates from remote first - it should avoid merge conflicts with the commit which happens below
Push-Location $cloudRoot
git pull origin master
Pop-Location

# Build the code - it gets built anyway during deploy but this validates it builds before pushing, and will execute any MSBuild steps
foreach ($folder in $foldersToCopy) {
    $folderPath = Resolve-Path (Join-Path $projectRoot -ChildPath $folder)
    if (Test-Path (Join-Path $folderPath -ChildPath $umbracoProjectFile)) {
        Push-Location $folderPath
        dotnet build
        Pop-Location
        break
    }
}

# Copy changes to the .UmbracoCloud deployment repository
Copy-Item $projectRoot\.umbraco $cloudRoot
foreach ($folder in $foldersToCopy) {
    $folderPath = Resolve-Path (Join-Path $projectRoot -ChildPath $folder)

    robocopy $folderPath $cloudRoot\src\$folder `
        /IF *.cs *.csproj *.cshtml *.uda *.css *.html *.js package.manifest en-*.xml *.png *.gif *.jpg *.svg *.ico *.woff *.woff2 *.lic appsettings.json appsettings.Production.json compilerconfig.json umbraco-cloud.json web.release.config `
        /XF member-group__*.uda *.test.js `
        /S `
        /XD $folderPath\obj $folderPath\bin $folderPath\node_modules $folderPath\sass $folderPath\App_Data $folderPath\wwwroot\media $folderPath\wwwroot\umbraco `
        $folderPath\App_Plugins\Deploy $folderPath\App_Plugins\UmbracoForms $folderPath\App_Plugins\UmbracoId $folderPath\App_Plugins\uSync `
        $folderPath\umbraco\config $folderPath\umbraco\Data $folderPath\umbraco\config\Logs $folderPath\umbraco\PartialViewMacros $folderPath\umbraco\UmbracoBackOffice `
        $folderPath\umbraco\UmbracoInstall $folderPath\umbraco\UmbracoWebsite $folderPath\Smidge

    # Update versions for each deploy where needed
    $version = (Get-Random).ToString();
    $targetFile = "$cloudRoot\src\$folder\wwwroot\service-worker.js"
    if (Test-Path $targetFile) {
        (Get-Content $targetFile) -Replace "version = '1.0.0'", "version = '$version'" | Set-Content $targetFile
    }
}    
    
    
# Commit and push those changes
Push-Location $cloudRoot
git add .
git commit -am "Changes from local site"
git push origin master
Pop-Location