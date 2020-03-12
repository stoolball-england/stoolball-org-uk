$projectRoot = Resolve-Path "$PSScriptRoot\.."
Push-Location $projectRoot

Write-Host "THIS COULD LOSE YOUR WORK. ARE YOU SURE?

* Do you have all your work committed or stashed on a branch?
* Have you pulled any changes from Umbraco Cloud into master using Pull-UmbracoCloud.ps1?
* If you deleted anything, have you also deleted it from the .UmbracoCloud repo? It might come back!

" -ForegroundColor Red
Read-Host -Prompt "Press any key to continue or CTRL+C to quit"

# Copy changes from Stoolball.Web to the .UmbracoCloud deployment repository
robocopy .\Stoolball.Web .\.UmbracoCloud /IF *.dll *.cshtml *.uda *.xdt.config *.css *.html /XF Umbraco.*.dll /S /XD .git $projectRoot\Stoolball.Web\obj $projectRoot\Stoolball.Web\umbraco $projectRoot\Stoolball.Web\App_Data $projectRoot\Stoolball.Web\App_Plugins\Deploy $projectRoot\Stoolball.Web\App_Plugins\UmbracoForms $projectRoot\Stoolball.Web\Content $projectRoot\Stoolball.Web\Scripts $projectRoot\Stoolball.Web\Media

# Commit and push those changes
Push-Location .\.UmbracoCloud
git add .
git commit -am "Changes from local site"
git push origin master
Pop-Location

Pop-Location