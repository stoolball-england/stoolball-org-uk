﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ASP
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Web;
    using System.Web.Helpers;
    using System.Web.Mvc;
    using System.Web.Mvc.Ajax;
    using System.Web.Mvc.Html;
    using System.Web.Routing;
    using System.Web.Security;
    using System.Web.UI;
    using System.Web.WebPages;
    
    #line 2 "..\..\Views\Partials\Forms\Themes\stoolball\Script.cshtml"
    using ClientDependency.Core.Mvc;
    
    #line default
    #line hidden
    using Examine;
    
    #line 3 "..\..\Views\Partials\Forms\Themes\stoolball\Script.cshtml"
    using Newtonsoft.Json;
    
    #line default
    #line hidden
    using Umbraco.Core;
    using Umbraco.Core.Models;
    using Umbraco.Core.Models.PublishedContent;
    
    #line 4 "..\..\Views\Partials\Forms\Themes\stoolball\Script.cshtml"
    using Umbraco.Forms.Mvc;
    
    #line default
    #line hidden
    using Umbraco.Web;
    using Umbraco.Web.Mvc;
    using Umbraco.Web.PublishedModels;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Views/Partials/Forms/Themes/stoolball/Script.cshtml")]
    public partial class _Views_Partials_Forms_Themes_stoolball_Script_cshtml : System.Web.Mvc.WebViewPage<Umbraco.Forms.Web.Models.FormViewModel>
    {
        public _Views_Partials_Forms_Themes_stoolball_Script_cshtml()
        {
        }
        public override void Execute()
        {
WriteLiteral("\r\n");

            
            #line 6 "..\..\Views\Partials\Forms\Themes\stoolball\Script.cshtml"
  
    var formJsObj = new
    {
        formId = Model.FormClientId,
        fieldSetConditions = Model.FieldsetConditions,
        fieldConditions = Model.FieldConditions,
        recordValues = Model.GetFieldsNotDisplayed()
    };

    Html.AddFormThemeScriptFile(Model, "~/App_Plugins/UmbracoForms/Assets/Themes/Default/umbracoforms.js");

            
            #line default
            #line hidden
WriteLiteral("\r\n\r\n<script>\r\n    if (typeof umbracoFormsCollection === \'undefined\') var umbracoF" +
"ormsCollection = [];\r\n    umbracoFormsCollection.push(\'");

            
            #line 20 "..\..\Views\Partials\Forms\Themes\stoolball\Script.cshtml"
                            Write(Html.Raw(Uri.EscapeUriString(JsonConvert.SerializeObject(formJsObj))));

            
            #line default
            #line hidden
WriteLiteral("\');\r\n</script>\r\n\r\n");

WriteLiteral("\r\n");

            
            #line 24 "..\..\Views\Partials\Forms\Themes\stoolball\Script.cshtml"
 if (Model.SubmitHandled == false)
{
    
            
            #line default
            #line hidden
            
            #line 29 "..\..\Views\Partials\Forms\Themes\stoolball\Script.cshtml"
      
    if (Model.CurrentPage.PartialViewFiles.Any())
    {
        foreach (var partial in Model.CurrentPage.PartialViewFiles)
        {
            
            
            #line default
            #line hidden
            
            #line 34 "..\..\Views\Partials\Forms\Themes\stoolball\Script.cshtml"
       Write(Html.Partial(partial.Value));

            
            #line default
            #line hidden
            
            #line 34 "..\..\Views\Partials\Forms\Themes\stoolball\Script.cshtml"
                                        
        }
    }


    
            
            #line default
            #line hidden
            
            #line 39 "..\..\Views\Partials\Forms\Themes\stoolball\Script.cshtml"
                                                                                   
    if (Model.UseClientDependency)
    {
        foreach (var script in Html.GetThemeScriptFiles(Model))
        {
            Html.RequiresJs(script.Value, Model.JavaScriptTagAttributes);
        }
        
        foreach (var css in Html.GetThemeCssFiles(Model))
        {
            Html.RequiresCss(css.Value);
        }
    }
    else
    {
        
            
            #line default
            #line hidden
            
            #line 54 "..\..\Views\Partials\Forms\Themes\stoolball\Script.cshtml"
   Write(Html.RenderFormsScripts(Url, Model, Model.JavaScriptTagAttributes));

            
            #line default
            #line hidden
            
            #line 54 "..\..\Views\Partials\Forms\Themes\stoolball\Script.cshtml"
                                                                           
        
            
            #line default
            #line hidden
            
            #line 55 "..\..\Views\Partials\Forms\Themes\stoolball\Script.cshtml"
   Write(Html.RenderFormsStylesheets(Url, Model));

            
            #line default
            #line hidden
            
            #line 55 "..\..\Views\Partials\Forms\Themes\stoolball\Script.cshtml"
                                                
    }
    
}

            
            #line default
            #line hidden
        }
    }
}
#pragma warning restore 1591
