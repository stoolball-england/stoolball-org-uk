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
    
    #line 2 "..\..\Views\MatchesForMatchLocation.cshtml"
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
    [System.Web.WebPages.PageVirtualPathAttribute("~/Views/MatchesForMatchLocation.cshtml")]
    public partial class _Views_MatchesForMatchLocation_cshtml : Umbraco.Web.Mvc.UmbracoViewPage<Stoolball.Web.MatchLocations.MatchLocationViewModel>
    {
        public _Views_MatchesForMatchLocation_cshtml()
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

WriteLiteral("<div");

WriteLiteral(" class=\"container-xl\"");

WriteLiteral(">\r\n    <h1>");

            
            #line 7 "..\..\Views\MatchesForMatchLocation.cshtml"
   Write(Model.MatchLocation.NameAndLocalityOrTownIfDifferent());

            
            #line default
            #line hidden
WriteLiteral("</h1>\r\n\r\n    <ul");

WriteLiteral(" class=\"nav nav-tabs\"");

WriteLiteral(">\r\n        <li");

WriteLiteral(" class=\"nav-item\"");

WriteLiteral(">\r\n            <a");

WriteLiteral(" class=\"nav-link\"");

WriteAttribute("href", Tuple.Create(" href=\"", 392), Tuple.Create("\"", 438)
            
            #line 11 "..\..\Views\MatchesForMatchLocation.cshtml"
, Tuple.Create(Tuple.Create("", 399), Tuple.Create<System.Object, System.Int32>(Model.MatchLocation.MatchLocationRoute
            
            #line default
            #line hidden
, 399), false)
);

WriteLiteral(">Summary</a>\r\n        </li>\r\n        <li");

WriteLiteral(" class=\"nav-item\"");

WriteLiteral(">\r\n            <em");

WriteLiteral(" class=\"nav-link active\"");

WriteLiteral(">Matches</em>\r\n        </li>\r\n        <li");

WriteLiteral(" class=\"nav-item\"");

WriteLiteral(">\r\n            <a");

WriteLiteral(" class=\"nav-link\"");

WriteAttribute("href", Tuple.Create(" href=\"", 630), Tuple.Create("\"", 687)
            
            #line 17 "..\..\Views\MatchesForMatchLocation.cshtml"
, Tuple.Create(Tuple.Create("", 637), Tuple.Create<System.Object, System.Int32>(Model.MatchLocation.MatchLocationRoute
            
            #line default
            #line hidden
, 637), false)
, Tuple.Create(Tuple.Create("", 676), Tuple.Create("/statistics", 676), true)
);

WriteLiteral(">Statistics</a>\r\n        </li>\r\n    </ul>\r\n\r\n");

            
            #line 21 "..\..\Views\MatchesForMatchLocation.cshtml"
    
            
            #line default
            #line hidden
            
            #line 21 "..\..\Views\MatchesForMatchLocation.cshtml"
     if (Model.Matches.Matches.Count > 0)
    {
        
            
            #line default
            #line hidden
            
            #line 23 "..\..\Views\MatchesForMatchLocation.cshtml"
   Write(Html.Partial("_MatchList", Model.Matches));

            
            #line default
            #line hidden
            
            #line 23 "..\..\Views\MatchesForMatchLocation.cshtml"
                                                  
    }
    else
    {

            
            #line default
            #line hidden
WriteLiteral("        <p>There are no matches at this location this season.</p>\r\n");

            
            #line 28 "..\..\Views\MatchesForMatchLocation.cshtml"
    }

            
            #line default
            #line hidden
WriteLiteral("    ");

            
            #line 29 "..\..\Views\MatchesForMatchLocation.cshtml"
Write(Html.Partial("_MatchListSubscriptions", new MatchListSubscriptionsViewModel { BaseRoute = Model.MatchLocation.MatchLocationRoute }));

            
            #line default
            #line hidden
WriteLiteral("\r\n</div>");

        }
    }
}
#pragma warning restore 1591
