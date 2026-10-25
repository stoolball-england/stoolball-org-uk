param(
    [string]$GitRemote
)

if (!$GitRemote) { $GitRemote = "origin" }

$projectRoot = Resolve-Path "$PSScriptRoot\.."
$cloudRoot = Resolve-Path "$projectRoot\.UmbracoCloud"

$foldersToCopy = "Stoolball", "Stoolball.Data.SqlServer", "Stoolball.Data.Abstractions", "Stoolball.Data.MemoryCache", "Stoolball.Data.UmbracoMigrations", "Stoolball.Web"

# The Playwright tests run against a copy of the site started by this script, so that they test the code being deployed.
# A site that's already running would lock the files the build needs to replace, and might not be running this code.
$siteUrl = "https://localhost:44343"
$sitePort = ([Uri]$siteUrl).Port
if (Get-NetTCPConnection -LocalPort $sitePort -State Listen -ErrorAction SilentlyContinue) {
    Write-Error "Something is already listening on port $sitePort. Stop the local site before deploying, so the Playwright tests run against the code being deployed."
    exit 1
}

# Get the path to the Umbraco project from the .umbraco Umbraco Cloud config file
$projectConfig = Get-Content $projectRoot/.umbraco
$configLine = $projectConfig | Where-Object { $_.StartsWith("csproj") } | Select-Object -First 1
if ($configLine -match 'csproj = "(?<csproj>[\w.-]+)"') {
    $umbracoProjectFile = $Matches.csproj
}


# Pull updates from remote first - it should avoid merge conflicts with the commit which happens below
Push-Location $cloudRoot
git pull $GitRemote master
Pop-Location

# Umbraco Cloud upgrades packages in its own repository. Copying older .csproj files over those upgrades stops Umbraco booting.
& "$PSScriptRoot\Assert-NoPackageDowngrade.ps1" -ProjectRoot $projectRoot -CloudRoot $cloudRoot -Folders $foldersToCopy
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Build the code - it gets built anyway during deploy but this validates it builds before pushing, and will execute any MSBuild steps
foreach ($folder in $foldersToCopy) {
    $folderPath = Resolve-Path (Join-Path $projectRoot -ChildPath $folder)
    if (Test-Path (Join-Path $folderPath -ChildPath $umbracoProjectFile)) {
        Push-Location $folderPath
        dotnet build
        $buildExitCode = $LASTEXITCODE
        Pop-Location
        if ($buildExitCode -ne 0) { exit $buildExitCode }
        $webProjectPath = $folderPath
        break
    }
}

# Run the full Playwright test suite against the site that was just built, and stop if any tests fail
$siteLog = Join-Path ([IO.Path]::GetTempPath()) "Push-UmbracoCloud-site.log"
$previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
$env:ASPNETCORE_ENVIRONMENT = "Development"
$site = Start-Process dotnet -ArgumentList "run", "--project", "`"$webProjectPath`"", "--no-build", "--no-launch-profile", "--urls", $siteUrl `
    -NoNewWindow -PassThru -RedirectStandardOutput $siteLog -RedirectStandardError "$siteLog.err"
$env:ASPNETCORE_ENVIRONMENT = $previousEnvironment

try {
    Write-Host "Waiting for the site to start at $siteUrl"
    $siteStarted = $false
    $deadline = (Get-Date).AddMinutes(10)
    while (!$site.HasExited -and (Get-Date) -lt $deadline) {
        $statusCode = curl.exe --silent --insecure --output NUL --write-out "%{http_code}" --max-time 60 $siteUrl
        if ($statusCode -eq "200") { $siteStarted = $true; break }
        Start-Sleep -Seconds 5
    }
    if (!$siteStarted) {
        Write-Error "The site did not start. See $siteLog and $siteLog.err"
        $testExitCode = 1
    }
    else {
        # The first request to each area of a newly started site is slow. Make those requests one at a time before the
        # tests run in parallel, so that they don't time out. Failures here are left for the tests to report.
        $warmUpPaths = "/account/activate/", "/matches", "/play/statistics", "/players/rick-mason", "/teams/maresfield-mixed",
            "/clubs/maresfield", "/locations/maresfield-recreation-ground", "/competitions/mid-sussex-mixed-league/2021",
            "/tournaments/slsa-tournament-23jun2013", "/news"
        foreach ($path in $warmUpPaths) {
            Write-Host "Warming up $path"
            curl.exe --silent --insecure --output NUL --max-time 120 "$siteUrl$path" | Out-Null
        }

        Push-Location $projectRoot
        $env:BASE_URL = $siteUrl
        npx playwright test -c Playwright
        $testExitCode = $LASTEXITCODE
        Remove-Item Env:BASE_URL
        Pop-Location
    }
}
finally {
    # dotnet run starts the site as a child process, so stop the whole process tree
    taskkill /PID $site.Id /T /F | Out-Null
}
if ($testExitCode -ne 0) {
    Write-Error "Playwright tests failed, so the site has not been deployed."
    exit $testExitCode
}

# Copy changes to the .UmbracoCloud deployment repository
Copy-Item $projectRoot\.umbraco $cloudRoot
foreach ($folder in $foldersToCopy) {
    $folderPath = Resolve-Path (Join-Path $projectRoot -ChildPath $folder)

    Get-ChildItem -Recurse $cloudRoot\src\$folder\*.cs | Remove-Item

    robocopy $folderPath $cloudRoot\src\$folder `
        /IF *.cs *.csproj *.cshtml *.uda *.css *.html *.js package.manifest en-*.xml *.png *.gif *.jpg *.svg *.ico *.woff *.woff2 *.lic *.sql appsettings.json appsettings.Production.json compilerconfig.json web.release.config robots.txt `
        /XF member-group__*.uda *.test.js umbraco-cloud.json `
        /S `
        /XD $folderPath\obj $folderPath\bin $folderPath\node_modules $folderPath\sass $folderPath\App_Data $folderPath\wwwroot\media `
        $folderPath\App_Plugins\Deploy $folderPath\App_Plugins\UmbracoId $folderPath\App_Plugins\uSync `
        $folderPath\umbraco\config $folderPath\umbraco\Data $folderPath\umbraco\config\Logs $folderPath\Smidge

    # Update versions for each deploy where needed
    $version = (Get-Random).ToString();
    $targetFile = "$cloudRoot\src\$folder\wwwroot\service-worker.js"
    if (Test-Path $targetFile) {
        (Get-Content $targetFile).Replace('version = "__ServiceWorkerVersion__"', "version = '$version'") | Set-Content $targetFile
    }

    $targetFile = "$cloudRoot\src\$folder\wwwroot\js\register-service-worker.js"
    if (Test-Path $targetFile) {
        (Get-Content $targetFile).Replace('__ServiceWorkerVersion__', "$version") | Set-Content $targetFile
    }

    $targetFile = "$cloudRoot\src\$folder\appsettings.Production.json"
    if (Test-Path $targetFile) {
        (Get-Content $targetFile).Replace("__SmidgeVersion__", "$version") | Set-Content $targetFile
    }
}    
    
    
# Commit and push those changes
Push-Location $cloudRoot
git add .
git commit -am "Changes from local site"
git push $GitRemote master
Pop-Location