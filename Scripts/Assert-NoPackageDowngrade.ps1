<#
.SYNOPSIS
Stops a deploy that would downgrade a NuGet package on Umbraco Cloud.

.DESCRIPTION
Umbraco Cloud upgrades Umbraco packages itself by committing new versions to the .csproj files in the Cloud repository.
Push-UmbracoCloud.ps1 copies the local .csproj files over those, so unless the upgrades were also made locally, a deploy
puts the old versions back. That's not safe: the database has already been migrated by the newer version, and an older
package can't run its migrations from that state, so Umbraco fails to boot and every page returns a 500 error.

This compares every PackageReference in the local projects with the highest version of that package referenced anywhere
in the Cloud repository, and fails if any local version is lower.

.PARAMETER ProjectRoot
The root of the local repository.

.PARAMETER CloudRoot
The root of the .UmbracoCloud deployment repository, after pulling the latest changes.

.PARAMETER Folders
The project folders that will be copied to the Cloud repository.
#>
param(
    [Parameter(Mandatory = $true)][string]$ProjectRoot,
    [Parameter(Mandatory = $true)][string]$CloudRoot,
    [Parameter(Mandatory = $true)][string[]]$Folders
)

function Get-PackageReferences([string]$projectFile) {
    [xml]$project = Get-Content $projectFile -Raw
    foreach ($reference in $project.SelectNodes("//PackageReference[@Include]")) {
        $version = $reference.GetAttribute("Version")
        if (!$version) {
            $versionElement = $reference.SelectSingleNode("Version")
            if ($versionElement) { $version = $versionElement.InnerText }
        }
        if ($version) {
            [PSCustomObject]@{ Package = $reference.GetAttribute("Include"); Version = $version.Trim(); Project = $projectFile }
        }
    }
}

function Get-RelativePath([string]$root, [string]$path) {
    $root = (Resolve-Path $root).Path.TrimEnd('\', '/') + '\'
    if ($path.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) { return $path.Substring($root.Length) }
    return $path
}

# Compares NuGet versions such as 13.16.2 and 2.0.0-beta1. Returns a negative number if $a is lower, positive if it's higher, 0 if equal.
function Compare-PackageVersion([string]$a, [string]$b) {
    $partsA = $a.Split('-', 2); $partsB = $b.Split('-', 2)
    $numberA = [version]::new(($partsA[0].Split('.') + '0', '0', '0')[0..2] -join '.')
    $numberB = [version]::new(($partsB[0].Split('.') + '0', '0', '0')[0..2] -join '.')
    $result = $numberA.CompareTo($numberB)
    if ($result -ne 0) { return $result }

    # A release is higher than a prerelease of the same version
    if ($partsA.Length -eq 1 -and $partsB.Length -eq 1) { return 0 }
    if ($partsA.Length -eq 1) { return 1 }
    if ($partsB.Length -eq 1) { return -1 }
    return [string]::CompareOrdinal($partsA[1], $partsB[1])
}

$cloudVersions = @{}
foreach ($projectFile in Get-ChildItem -Path (Join-Path $CloudRoot "src") -Recurse -Filter *.csproj) {
    foreach ($reference in Get-PackageReferences $projectFile.FullName) {
        $highest = $cloudVersions[$reference.Package]
        if (!$highest -or (Compare-PackageVersion $reference.Version $highest.Version) -gt 0) {
            $cloudVersions[$reference.Package] = $reference
        }
    }
}

$downgrades = @()
foreach ($folder in $Folders) {
    foreach ($projectFile in Get-ChildItem -Path (Join-Path $ProjectRoot $folder) -Filter *.csproj) {
        foreach ($reference in Get-PackageReferences $projectFile.FullName) {
            $cloud = $cloudVersions[$reference.Package]
            if ($cloud -and (Compare-PackageVersion $reference.Version $cloud.Version) -lt 0) {
                $downgrades += [PSCustomObject]@{
                    Package      = $reference.Package
                    Local        = $reference.Version
                    Cloud        = $cloud.Version
                    LocalProject = Get-RelativePath $ProjectRoot $reference.Project
                    CloudProject = Get-RelativePath $CloudRoot $cloud.Project
                }
            }
        }
    }
}

if ($downgrades.Count -gt 0) {
    Write-Host "This deploy would downgrade packages that Umbraco Cloud has already upgraded:" -ForegroundColor Red
    $downgrades | Format-Table -AutoSize | Out-String -Width 300 | Write-Host
    Write-Host "Update the local projects to at least the Cloud versions, then deploy again. Nothing has been copied or pushed." -ForegroundColor Red
    exit 1
}

Write-Host "No package downgrades found."
exit 0
