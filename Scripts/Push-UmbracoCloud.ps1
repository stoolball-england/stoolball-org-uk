$projectRoot = Resolve-Path "$PSScriptRoot\.."
Push-Location $projectRoot

Write-Host "YOU SHOULD ALWAYS BE WORKING ON A BRANCH AND HAVE EVERYTHING COMMITTED OR STASHED.
CHANGES FROM REMOTE WILL NOW OVERWRITE ANYTHING, EVEN COMMITS, ON `DEVELOP`
DOING THIS BEFORE A MERGE TO `DEVELOP` AVOIDS REMOTE CHANGES OVERWRITING LOCAL.

ALSO, IF YOU DELETED ANYTHING DELETE IT FROM THE .UmbracoCloud REPO TOO, OR IT'LL COME BACK!!!" -ForegroundColor Red
Read-Host -Prompt "Press any key to continue or CTRL+C to quit" 

# Ensure develop branch is up-to-date with changes from Umbraco Cloud
.\Scripts\Pull-UmbracoCloud

# Copy changes from Stoolball.Web to the .UmbracoCloud deployment repository
robocopy .\Stoolball.Web .\.UmbracoCloud /IF *.dll *.cshtml *.uda Web.config *.xdt.config /XF Umbraco.*.dll /S /XD .git $projectRoot\Stoolball.Web\obj $projectRoot\Stoolball.Web\umbraco $projectRoot\Stoolball.Web\App_Data $projectRoot\Stoolball.Web\App_Plugins\Deploy $projectRoot\Stoolball.Web\App_Plugins\UmbracoForms $projectRoot\Stoolball.Web\Content $projectRoot\Stoolball.Web\Scripts $projectRoot\Stoolball.Web\Media

# Commit and push those changes
Push-Location .\.UmbracoCloud
git add .
git commit -am "Changes from local site"
git push origin master
Pop-Location

Pop-Location