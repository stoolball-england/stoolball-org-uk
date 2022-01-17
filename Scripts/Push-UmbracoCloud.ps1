$projectRoot = Resolve-Path "$PSScriptRoot\.."

# Get the path to the Umbraco code from the .umbraco Umbraco Cloud config file
$projectConfig = Get-Content $projectRoot/.umbraco
$configLine = $projectConfig | Where-Object { $_.StartsWith("base") } | Select-Object -First 1
if ($configLine -match 'base = "(?<UmbracoRoot>src/[\w.-]+)"') {
    $umbracoProjectPath = $Matches.UmbracoRoot
    $umbracoRoot = Resolve-Path "$projectRoot\Stoolball.Web"
    $cloudRoot = Resolve-Path "$projectRoot\.UmbracoCloud"
    
    # Pull updates from remote first - it should avoid merge conflicts with the commit which happens below
    Push-Location $cloudRoot
    git pull origin master
    Pop-Location
    
    # Build the code - it gets built anyway during deploy but this validates it builds before pushing, and will execute any MSBuild steps
    Push-Location $umbracoRoot
    dotnet build
    Pop-Location
    
    # Copy changes from $umbracoRoot to the .UmbracoCloud deployment repository
    robocopy $umbracoRoot $cloudRoot\$umbracoProjectPath `
        /IF *.cs *.csproj *.cshtml *.uda *.css *.html *.js package.manifest en-*.xml *.png *.gif *.jpg *.svg *.ico *.woff *.woff2 appsettings.json `
        /XF Umbraco.*.dll uSync*.dll member-group__*.uda *.test.js `
        /S `
        /XD .git $umbracoRoot\obj $umbracoRoot\bin $umbracoRoot\node_modules $umbracoRoot\sass $umbracoRoot\App_Data $umbracoRoot\wwwroot\media $umbracoRoot\wwwroot\umbraco `
        $umbracoRoot\App_Plugins\Deploy $umbracoRoot\App_Plugins\UmbracoForms $umbracoRoot\App_Plugins\UmbracoId $umbracoRoot\App_Plugins\uSync `
        $umbracoRoot\umbraco\config $umbracoRoot\umbraco\Data $umbracoRoot\umbraco\config\Logs $umbracoRoot\umbraco\PartialViewMacros $umbracoRoot\umbraco\UmbracoBackOffice `
        $umbracoRoot\umbraco\UmbracoInstall $umbracoRoot\umbraco\UmbracoWebsite
    
    # Update versions for each deploy where needed
    # $version = (Get-Random).ToString();
    # (Get-Content "$cloudRoot\$umbracoProjectPath\wwwroot\service-worker.js") -Replace "version = '1.0.0'", "version = '$version'" | Set-Content "$cloudRoot\$umbracoProjectPath\wwwroot\service-worker.js"
    
    # Commit and push those changes
    Push-Location $cloudRoot
    git add .
    git commit -am "Changes from local site"
    git push origin master
    Pop-Location
}