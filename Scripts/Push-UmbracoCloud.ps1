$projectRoot = Resolve-Path "$PSScriptRoot\.."
Push-Location $projectRoot

# Copy changes from Stoolball.Web to the .UmbracoCloud deployment repository
robocopy .\Stoolball.Web .\.UmbracoCloud /IF *.dll *.cshtml *.uda *.xdt.config *.css *.html *.js package.manifest en-*.xml *.png *.gif *.jpg *.svg *.woff *.woff2 /XF Umbraco.*.dll uSync8.*.dll member-group__*.uda *.local.xdt.config *.test.js /S /XD .git $projectRoot\Stoolball.Web\obj $projectRoot\Stoolball.Web\umbraco $projectRoot\Stoolball.Web\App_Data $projectRoot\Stoolball.Web\App_Plugins\Deploy $projectRoot\Stoolball.Web\App_Plugins\UmbracoForms $projectRoot\Stoolball.Web\App_Plugins\uSync8 $projectRoot\Stoolball.Web\Content $projectRoot\Stoolball.Web\Media
copy .\Stoolball.Web\fonts\Web.config .\.UmbracoCloud\fonts
copy .\Stoolball.Web\images\Web.config .\.UmbracoCloud\images

# Update ClientDependency.config version
$xml = [xml](Select-Xml -Path "$projectRoot\.UmbracoCloud\config\ClientDependency.config" -XPath /).Node; 
$node = $xml.SelectSingleNode("/clientDependency")
$node.SetAttribute("version", (Get-Random).ToString())
$xml.Save("$projectRoot\.UmbracoCloud\config\ClientDependency.config")

# Commit and push those changes
Push-Location .\.UmbracoCloud
git add .
git commit -am "Changes from local site"
git pull origin master
git push origin master
Pop-Location

Pop-Location