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
    
    #line 2 "..\..\Views\SeasonMap.cshtml"
    using ClientDependency.Core.Mvc;
    
    #line default
    #line hidden
    using Examine;
    
    #line 3 "..\..\Views\SeasonMap.cshtml"
    using Stoolball.Matches;
    
    #line default
    #line hidden
    using Umbraco.Core;
    using Umbraco.Core.Models;
    using Umbraco.Core.Models.PublishedContent;
    using Umbraco.Web;
    using Umbraco.Web.Mvc;
    using Umbraco.Web.PublishedModels;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Views/SeasonMap.cshtml")]
    public partial class _Views_SeasonMap_cshtml : Umbraco.Web.Mvc.UmbracoViewPage<Stoolball.Web.Competitions.SeasonViewModel>
    {
        public _Views_SeasonMap_cshtml()
        {
        }
        public override void Execute()
        {
            
            #line 4 "..\..\Views\SeasonMap.cshtml"
  
    Html.RequiresJs("https://maps.google.co.uk/maps/api/js?key=" + Model.GoogleMapsApiKey, 50);
    Html.RequiresJs("/js/maps.js", 90);
    Html.RequiresJs("/js/libs/markerclustererplus.1.0.3.min.js", 95);
    Html.RequiresJs($"/teams/teams-map.js?season={Model.Season.SeasonId}&adjust=false");

            
            #line default
            #line hidden
WriteLiteral("\r\n<div");

WriteLiteral(" class=\"container-xl\"");

WriteLiteral(">\r\n    <h1>");

            
            #line 11 "..\..\Views\SeasonMap.cshtml"
   Write(Model.Season.SeasonFullNameAndPlayerType());

            
            #line default
            #line hidden
WriteLiteral("</h1>\r\n\r\n");

            
            #line 13 "..\..\Views\SeasonMap.cshtml"
    
            
            #line default
            #line hidden
            
            #line 13 "..\..\Views\SeasonMap.cshtml"
     if (Model.Season.Competition.UntilYear.HasValue)
    {

            
            #line default
            #line hidden
WriteLiteral("        <div");

WriteLiteral(" class=\"alert alert-info\"");

WriteLiteral(">\r\n            <p><strong>This competition isn\'t played any more.</strong></p>\r\n " +
"       </div>\r\n");

            
            #line 18 "..\..\Views\SeasonMap.cshtml"
    }

            
            #line default
            #line hidden
WriteLiteral("\r\n    <ul");

WriteLiteral(" class=\"nav nav-tabs\"");

WriteLiteral(">\r\n        <li");

WriteLiteral(" class=\"nav-item\"");

WriteLiteral(">\r\n            <a");

WriteLiteral(" class=\"nav-link\"");

WriteAttribute("href", Tuple.Create(" href=\"", 838), Tuple.Create("\"", 870)
            
            #line 22 "..\..\Views\SeasonMap.cshtml"
, Tuple.Create(Tuple.Create("", 845), Tuple.Create<System.Object, System.Int32>(Model.Season.SeasonRoute
            
            #line default
            #line hidden
, 845), false)
);

WriteLiteral(">Summary</a>\r\n        </li>\r\n        <li");

WriteLiteral(" class=\"nav-item\"");

WriteLiteral(">\r\n            <a");

WriteLiteral(" class=\"nav-link\"");

WriteAttribute("href", Tuple.Create(" href=\"", 962), Tuple.Create("\"", 1002)
            
            #line 25 "..\..\Views\SeasonMap.cshtml"
, Tuple.Create(Tuple.Create("", 969), Tuple.Create<System.Object, System.Int32>(Model.Season.SeasonRoute
            
            #line default
            #line hidden
, 969), false)
, Tuple.Create(Tuple.Create("", 994), Tuple.Create("/matches", 994), true)
);

WriteLiteral(">Matches</a>\r\n        </li>\r\n        <li");

WriteLiteral(" class=\"nav-item\"");

WriteLiteral(">\r\n            <em");

WriteLiteral(" class=\"nav-link active\"");

WriteLiteral(">Map</em>\r\n        </li>\r\n");

            
            #line 30 "..\..\Views\SeasonMap.cshtml"
        
            
            #line default
            #line hidden
            
            #line 30 "..\..\Views\SeasonMap.cshtml"
         if (Model.Season.MatchTypes.Contains(MatchType.LeagueMatch) ||
            Model.Season.MatchTypes.Contains(MatchType.KnockoutMatch) ||
            Model.Season.MatchTypes.Contains(MatchType.FriendlyMatch) ||
            !string.IsNullOrEmpty(Model.Season.Results))
        {

            
            #line default
            #line hidden
WriteLiteral("            <li");

WriteLiteral(" class=\"nav-item\"");

WriteLiteral(">\r\n                <a");

WriteLiteral(" class=\"nav-link\"");

WriteAttribute("href", Tuple.Create(" href=\"", 1488), Tuple.Create("\"", 1526)
            
            #line 36 "..\..\Views\SeasonMap.cshtml"
, Tuple.Create(Tuple.Create("", 1495), Tuple.Create<System.Object, System.Int32>(Model.Season.SeasonRoute
            
            #line default
            #line hidden
, 1495), false)
, Tuple.Create(Tuple.Create("", 1520), Tuple.Create("/table", 1520), true)
);

WriteLiteral(">Table</a>\r\n            </li>\r\n");

            
            #line 38 "..\..\Views\SeasonMap.cshtml"
        }

            
            #line default
            #line hidden
WriteLiteral("        <li");

WriteLiteral(" class=\"nav-item\"");

WriteLiteral(">\r\n            <a");

WriteLiteral(" class=\"nav-link\"");

WriteAttribute("href", Tuple.Create(" href=\"", 1631), Tuple.Create("\"", 1674)
            
            #line 40 "..\..\Views\SeasonMap.cshtml"
, Tuple.Create(Tuple.Create("", 1638), Tuple.Create<System.Object, System.Int32>(Model.Season.SeasonRoute
            
            #line default
            #line hidden
, 1638), false)
, Tuple.Create(Tuple.Create("", 1663), Tuple.Create("/statistics", 1663), true)
);

WriteLiteral(">Statistics</a>\r\n        </li>\r\n    </ul>\r\n\r\n    <div");

WriteLiteral(" id=\"map\"");

WriteLiteral(">\r\n        <p");

WriteLiteral(" class=\"alert-danger alert\"");

WriteLiteral(">\r\n            <strong>\r\n                You can view teams in this season using " +
"Google Maps,\r\n                but we can only show you the map if you <a");

WriteLiteral(" href=\"/privacy/cookies/\"");

WriteLiteral(">consent to maps</a>.\r\n            </strong>\r\n        </p>\r\n    </div>\r\n</div>");

        }
    }
}
#pragma warning restore 1591
