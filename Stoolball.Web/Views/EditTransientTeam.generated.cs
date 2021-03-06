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
    
    #line 2 "..\..\Views\EditTransientTeam.cshtml"
    using ClientDependency.Core.Mvc;
    
    #line default
    #line hidden
    using Examine;
    
    #line 4 "..\..\Views\EditTransientTeam.cshtml"
    using Stoolball.Security;
    
    #line default
    #line hidden
    
    #line 3 "..\..\Views\EditTransientTeam.cshtml"
    using Stoolball.Web.Teams;
    
    #line default
    #line hidden
    using Umbraco.Core;
    using Umbraco.Core.Models;
    using Umbraco.Core.Models.PublishedContent;
    using Umbraco.Web;
    using Umbraco.Web.Mvc;
    using Umbraco.Web.PublishedModels;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Views/EditTransientTeam.cshtml")]
    public partial class _Views_EditTransientTeam_cshtml : Umbraco.Web.Mvc.UmbracoViewPage<TeamViewModel>
    {
        public _Views_EditTransientTeam_cshtml()
        {
        }
        public override void Execute()
        {
DefineSection("head", () => {

WriteLiteral("\r\n    <meta");

WriteLiteral(" name=\"robots\"");

WriteLiteral(" content=\"noindex,follow\"");

WriteLiteral(" />\r\n");

});

            
            #line 8 "..\..\Views\EditTransientTeam.cshtml"
  
    Html.EnableClientValidation();
    Html.EnableUnobtrusiveJavaScript();
    Html.RequiresJs("~/scripts/jquery.validate.min.js");
    Html.RequiresJs("~/scripts/jquery.validate.unobtrusive.min.js");

    Html.RequiresJs("/umbraco/lib/tinymce/tinymce.min.js", 90);
    Html.RequiresJs("/js/tinymce.js");

    var firstAndOnlyMatch = Model.Matches.Matches.FirstOrDefault();

            
            #line default
            #line hidden
WriteLiteral("\r\n<div");

WriteLiteral(" class=\"container-xl\"");

WriteLiteral(">\r\n    <h1>Edit ");

            
            #line 20 "..\..\Views\EditTransientTeam.cshtml"
        Write(Model.Team.TeamName);

            
            #line default
            #line hidden
WriteLiteral(", ");

            
            #line 20 "..\..\Views\EditTransientTeam.cshtml"
                              Write(Model.Matches.DateTimeFormatter.FormatDate(firstAndOnlyMatch.StartTime, false, false));

            
            #line default
            #line hidden
WriteLiteral("</h1>\r\n\r\n");

            
            #line 22 "..\..\Views\EditTransientTeam.cshtml"
    
            
            #line default
            #line hidden
            
            #line 22 "..\..\Views\EditTransientTeam.cshtml"
     if (Model.IsAuthorized[AuthorizedAction.EditTeam])
    {
        using (Html.BeginUmbracoForm<EditTransientTeamSurfaceController>
            ("UpdateTransientTeam"))
        {
            
            
            #line default
            #line hidden
            
            #line 27 "..\..\Views\EditTransientTeam.cshtml"
       Write(Html.AntiForgeryToken());

            
            #line default
            #line hidden
            
            #line 27 "..\..\Views\EditTransientTeam.cshtml"
                                    
            
            
            #line default
            #line hidden
            
            #line 28 "..\..\Views\EditTransientTeam.cshtml"
       Write(Html.Partial("_CreateOrEditTeam"));

            
            #line default
            #line hidden
            
            #line 28 "..\..\Views\EditTransientTeam.cshtml"
                                              
        }
    }
    else
    {
        
            
            #line default
            #line hidden
            
            #line 33 "..\..\Views\EditTransientTeam.cshtml"
   Write(Html.Partial("_Login"));

            
            #line default
            #line hidden
            
            #line 33 "..\..\Views\EditTransientTeam.cshtml"
                               
    }

            
            #line default
            #line hidden
WriteLiteral("</div>");

        }
    }
}
#pragma warning restore 1591
