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
    using Examine;
    
    #line 2 "..\..\Views\Tournaments.cshtml"
    using Stoolball.Web.Matches;
    
    #line default
    #line hidden
    using Umbraco.Core;
    using Umbraco.Core.Models;
    using Umbraco.Core.Models.PublishedContent;
    using Umbraco.Web;
    using Umbraco.Web.Mvc;
    using Umbraco.Web.PublishedModels;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Views/Tournaments.cshtml")]
    public partial class _Views_Tournaments_cshtml : Umbraco.Web.Mvc.UmbracoViewPage<MatchListingViewModel>
    {
        public _Views_Tournaments_cshtml()
        {
        }
        public override void Execute()
        {
DefineSection("canonical", () => {

            
            #line 3 "..\..\Views\Tournaments.cshtml"
               Write(Html.Partial("_CanonicalUrl", new[] { "page", "q" }));

            
            #line default
            #line hidden
});

WriteLiteral("<div");

WriteLiteral(" class=\"container-xl\"");

WriteLiteral(">\r\n    <h1>");

            
            #line 5 "..\..\Views\Tournaments.cshtml"
   Write(Stoolball.Constants.Pages.Tournaments);

            
            #line default
            #line hidden
WriteLiteral("</h1>\r\n\r\n    <ul");

WriteLiteral(" class=\"nav nav-tabs nav-tabs-has-add\"");

WriteLiteral(">\r\n        <li");

WriteLiteral(" class=\"nav-item\"");

WriteLiteral(">\r\n            <a");

WriteLiteral(" href=\"/matches\"");

WriteLiteral(" class=\"nav-link\"");

WriteLiteral(">Matches</a>\r\n        </li>\r\n        <li");

WriteLiteral(" class=\"nav-item\"");

WriteLiteral(">\r\n            <em");

WriteLiteral(" class=\"nav-link active\"");

WriteLiteral(">Tournaments</em>\r\n        </li>\r\n    </ul>\r\n\r\n");

            
            #line 16 "..\..\Views\Tournaments.cshtml"
    
            
            #line default
            #line hidden
            
            #line 16 "..\..\Views\Tournaments.cshtml"
     if (Model.Matches.Count > 0)
    {
        
            
            #line default
            #line hidden
            
            #line 18 "..\..\Views\Tournaments.cshtml"
   Write(Html.Partial("_MatchList", Model));

            
            #line default
            #line hidden
            
            #line 18 "..\..\Views\Tournaments.cshtml"
                                          
    }
    else
    {

            
            #line default
            #line hidden
WriteLiteral("        <p>There are no tournaments yet this season.</p>\r\n");

            
            #line 23 "..\..\Views\Tournaments.cshtml"
    }

            
            #line default
            #line hidden
WriteLiteral("\r\n");

WriteLiteral("    ");

            
            #line 25 "..\..\Views\Tournaments.cshtml"
Write(Html.Partial("_Paging", Model.MatchFilter.Paging));

            
            #line default
            #line hidden
WriteLiteral("\r\n");

WriteLiteral("    ");

            
            #line 26 "..\..\Views\Tournaments.cshtml"
Write(Html.Partial("_MatchListSubscriptions", new MatchListSubscriptionsViewModel { FilenameWithoutExtension = "tournaments" }));

            
            #line default
            #line hidden
WriteLiteral("\r\n</div>");

        }
    }
}
#pragma warning restore 1591