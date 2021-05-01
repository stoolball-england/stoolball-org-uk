$projectRoot = Resolve-Path "$PSScriptRoot\.."
Push-Location $projectRoot

# Pull updates from remote first - it should avoid merge conflicts with the commit which happens below
Push-Location .\.UmbracoCloud
git pull origin master
Pop-Location

# Copy changes from Stoolball.Web to the .UmbracoCloud deployment repository
robocopy .\Stoolball.Web .\.UmbracoCloud /IF *.dll *.cshtml *.uda *.xdt.config *.css *.html *.js package.manifest en-*.xml *.png *.gif *.jpg *.svg *.ico *.woff *.woff2 /XF Umbraco.*.dll uSync8.*.dll member-group__*.uda *.local.xdt.config *.template.config *.test.js /S /XD .git $projectRoot\Stoolball.Web\obj $projectRoot\Stoolball.Web\umbraco $projectRoot\Stoolball.Web\App_Data $projectRoot\Stoolball.Web\App_Plugins\Deploy $projectRoot\Stoolball.Web\App_Plugins\UmbracoForms $projectRoot\Stoolball.Web\App_Plugins\uSync8 $projectRoot\Stoolball.Web\Content $projectRoot\Stoolball.Web\Media
copy .\Stoolball.Web\App_Plugins\UmbracoForms\Backoffice\Common\FieldTypes\EmailField.html .\.UmbracoCloud\App_Plugins\UmbracoForms\Backoffice\Common\FieldTypes
copy .\Stoolball.Web\ApplicationInsights.template.config .\.UmbracoCloud\ApplicationInsights.config
copy .\Stoolball.Web\fonts\Web.config .\.UmbracoCloud\fonts
copy .\Stoolball.Web\images\Web.config .\.UmbracoCloud\images

# Update ClientDependency.config and service worker version
$version = (Get-Random).ToString();
$xml = [xml](Select-Xml -Path "$projectRoot\.UmbracoCloud\config\ClientDependency.config" -XPath /).Node; 
$node = $xml.SelectSingleNode("/clientDependency")
$node.SetAttribute("version", $version)
$xml.Save("$projectRoot\.UmbracoCloud\config\ClientDependency.config")
(Get-Content "$projectRoot\.UmbracoCloud\service-worker.js") -Replace "version = '1.0.0'", "version = '$version'" | Set-Content "$projectRoot\.UmbracoCloud\service-worker.js"

# Commit and push those changes
Push-Location .\.UmbracoCloud
git add .
git commit -am "Changes from local site"
git push origin master
Pop-Location

Pop-Location