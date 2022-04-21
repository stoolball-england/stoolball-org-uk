param(
    [Parameter(Mandatory = $True)]
    $ServerName,
    [Parameter(Mandatory = $True)]
    $Database,
    [Parameter(Mandatory = $True)]
    $Login,
    [Parameter(Mandatory = $True)]
    $Password
)

# Location of Microsoft.SqlServer.Dac.dll
$dacAssembly = $env:userprofile + "\.nuget\packages\microsoft.sqlserver.dacfx\150.4897.1\lib\net46\Microsoft.SqlServer.Dac.dll"

# Connection details can be found under "Settings" > "Connection details" on Umbraco.io
# If your password contains $ you'll have to escape them with a backtick (`)
$ConnectionString = "server=$ServerName;database=$Database;user id=$Login;password=$Password"

# Bacpac files will be written to this directory 
$backupDirectory = Join-Path -Path $PSScriptRoot -ChildPath "..\Backups"
If (!(Test-Path $backupDirectory -PathType Container)) {
    New-Item $backupDirectory -ItemType Directory | Out-Null
}

# Load DAC assembly
Write-Host "Loading Dac Assembly: $dacAssembly"
Add-Type -Path $dacAssembly
Write-Host "Dac Assembly loaded."

# Initialize Dac
$now = $(Get-Date).ToString("HH:mm:ss")
$services = new-object Microsoft.SqlServer.Dac.DacServices $ConnectionString
if ($null -eq $services) {
    Write-Error "Error connecting to database"
    exit
}

$dateTime = $(Get-Date).ToString("yyyy-MM-dd-HH.mm.ss")

Write-Host "Starting backup of $Database at $now"
$watch = New-Object System.Diagnostics.StopWatch
$watch.Start()

$bacpac = (Join-Path -Path $backupDirectory -ChildPath "$Database$dateTime.bacpac")

$tables = New-Object 'Collections.Generic.List[Tuple[string,string]]'

# Umbraco 
$tables.Add([System.Tuple]::Create("dbo", "cmsContentNu"))
$tables.Add([System.Tuple]::Create("dbo", "cmsContentType"))
$tables.Add([System.Tuple]::Create("dbo", "cmsContentType2ContentType"))
$tables.Add([System.Tuple]::Create("dbo", "cmsContentTypeAllowedContentType"))
$tables.Add([System.Tuple]::Create("dbo", "cmsDictionary"))
$tables.Add([System.Tuple]::Create("dbo", "cmsDocumentType"))
$tables.Add([System.Tuple]::Create("dbo", "cmsLanguageText"))
$tables.Add([System.Tuple]::Create("dbo", "cmsMacro"))
$tables.Add([System.Tuple]::Create("dbo", "cmsMacroProperty"))
$tables.Add([System.Tuple]::Create("dbo", "cmsMember"))
$tables.Add([System.Tuple]::Create("dbo", "cmsMember2MemberGroup"))
$tables.Add([System.Tuple]::Create("dbo", "cmsMemberType"))
$tables.Add([System.Tuple]::Create("dbo", "cmsPropertyType"))
$tables.Add([System.Tuple]::Create("dbo", "cmsPropertyTypeGroup"))
$tables.Add([System.Tuple]::Create("dbo", "cmsTagRelationship"))
$tables.Add([System.Tuple]::Create("dbo", "cmsTags"))
$tables.Add([System.Tuple]::Create("dbo", "cmsTemplate"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoAccess"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoAccessRule"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoAudit"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoCacheInstruction"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoConsent"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoContent"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoContentSchedule"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoContentVersion"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoContentVersionCultureVariation"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoDataType"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoDeployDependency"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoDeploySignature"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoDocument"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoDocumentCultureVariation"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoDocumentVersion"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoDomain"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoExternalLogin"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoKeyValue"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoLanguage"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoLock"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoLog"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoMediaVersion"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoNode"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoPropertyData"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoRedirectUrl"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoRelation"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoRelationType"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoServer"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoUser"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoUser2NodeNotify"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoUser2UserGroup"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoUserGroup"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoUserGroup2App"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoUserGroup2NodePermission"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoUserLogin"))
$tables.Add([System.Tuple]::Create("dbo", "umbracoUserStartNode"))

# Umbraco Cloud
$tables.Add([System.Tuple]::Create("dbo", "UCErrorLog"))

# Umbraco Forms
$tables.Add([System.Tuple]::Create("dbo", "UFFolders"))
$tables.Add([System.Tuple]::Create("dbo", "UFDataSource"))
$tables.Add([System.Tuple]::Create("dbo", "UFForms"))
$tables.Add([System.Tuple]::Create("dbo", "UFPrevalueSource"))
$tables.Add([System.Tuple]::Create("dbo", "UFRecordDataBit"))
$tables.Add([System.Tuple]::Create("dbo", "UFRecordDataDateTime"))
$tables.Add([System.Tuple]::Create("dbo", "UFRecordDataInteger"))
$tables.Add([System.Tuple]::Create("dbo", "UFRecordDataLongString"))
$tables.Add([System.Tuple]::Create("dbo", "UFRecordDataString"))
$tables.Add([System.Tuple]::Create("dbo", "UFRecordFields"))
$tables.Add([System.Tuple]::Create("dbo", "UFRecords"))
$tables.Add([System.Tuple]::Create("dbo", "UFUserFormSecurity"))
$tables.Add([System.Tuple]::Create("dbo", "UFUserSecurity"))
$tables.Add([System.Tuple]::Create("dbo", "UFWorkflows"))

# Stoolball data (except StoolballAudit which takes too long)
$tables.Add([System.Tuple]::Create("dbo", "SkybrudRedirects"))
$tables.Add([System.Tuple]::Create("dbo", "StoolballAward"))
$tables.Add([System.Tuple]::Create("dbo", "StoolballAwardedTo"))
$tables.Add([System.Tuple]::Create("dbo", "StoolballBowlingFigures"))
$tables.Add([System.Tuple]::Create("dbo", "StoolballClub"))
$tables.Add([System.Tuple]::Create("dbo", "StoolballClubVersion"))
$tables.Add([System.Tuple]::Create("dbo", "StoolballComment"))
$tables.Add([System.Tuple]::Create("dbo", "StoolballCompetition"))
$tables.Add([System.Tuple]::Create("dbo", "StoolballCompetitionVersion"))
$tables.Add([System.Tuple]::Create("dbo", "StoolballFallOfWicket"))
$tables.Add([System.Tuple]::Create("dbo", "StoolballMatch"))
$tables.Add([System.Tuple]::Create("dbo", "StoolballMatchInnings"))
$tables.Add([System.Tuple]::Create("dbo", "StoolballMatchLocation"))
$tables.Add([System.Tuple]::Create("dbo", "StoolballMatchTeam"))
$tables.Add([System.Tuple]::Create("dbo", "StoolballNotificationSubscription"))
$tables.Add([System.Tuple]::Create("dbo", "StoolballOver"))
$tables.Add([System.Tuple]::Create("dbo", "StoolballOverSet"))
$tables.Add([System.Tuple]::Create("dbo", "StoolballPlayer"))
$tables.Add([System.Tuple]::Create("dbo", "StoolballPlayerIdentity"))
$tables.Add([System.Tuple]::Create("dbo", "StoolballPlayerInMatchStatistics"))
$tables.Add([System.Tuple]::Create("dbo", "StoolballPlayerInnings"))
$tables.Add([System.Tuple]::Create("dbo", "StoolballPointsAdjustment"))
$tables.Add([System.Tuple]::Create("dbo", "StoolballPointsRule"))
$tables.Add([System.Tuple]::Create("dbo", "StoolballSchool"))
$tables.Add([System.Tuple]::Create("dbo", "StoolballSchoolVersion"))
$tables.Add([System.Tuple]::Create("dbo", "StoolballSeason"))
$tables.Add([System.Tuple]::Create("dbo", "StoolballSeasonMatchType"))
$tables.Add([System.Tuple]::Create("dbo", "StoolballSeasonTeam"))
$tables.Add([System.Tuple]::Create("dbo", "StoolballTeam"))
$tables.Add([System.Tuple]::Create("dbo", "StoolballTeamMatchLocation"))
$tables.Add([System.Tuple]::Create("dbo", "StoolballTeamVersion"))
$tables.Add([System.Tuple]::Create("dbo", "StoolballTournament"))
$tables.Add([System.Tuple]::Create("dbo", "StoolballTournamentSeason"))
$tables.Add([System.Tuple]::Create("dbo", "StoolballTournamentTeam"))

$services.ExportBacpac($bacpac, $Database, $tables)
$watch.Stop()
Write-Host "Backup completed in" $watch.Elapsed.ToString()