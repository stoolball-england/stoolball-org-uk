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
    
    #line 2 "..\..\Views\Teams.cshtml"
    using Humanizer;
    
    #line default
    #line hidden
    
    #line 3 "..\..\Views\Teams.cshtml"
    using Stoolball.Teams;
    
    #line default
    #line hidden
    using Umbraco.Core;
    using Umbraco.Core.Models;
    using Umbraco.Core.Models.PublishedContent;
    using Umbraco.Web;
    using Umbraco.Web.Mvc;
    using Umbraco.Web.PublishedModels;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Views/Teams.cshtml")]
    public partial class _Views_Teams_cshtml : Umbraco.Web.Mvc.UmbracoViewPage<Stoolball.Web.Teams.TeamsViewModel>
    {
        public _Views_Teams_cshtml()
        {
        }
        public override void Execute()
        {
WriteLiteral("<div");

WriteLiteral(" class=\"container-xl\"");

WriteLiteral(">\r\n    <h1>\r\n");

WriteLiteral("        ");

            
            #line 6 "..\..\Views\Teams.cshtml"
   Write(Stoolball.Constants.Pages.Teams);

            
            #line default
            #line hidden
WriteLiteral("\r\n");

            
            #line 7 "..\..\Views\Teams.cshtml"
        
            
            #line default
            #line hidden
            
            #line 7 "..\..\Views\Teams.cshtml"
         if (!string.IsNullOrEmpty(Model.TeamFilter.Query))
        {

            
            #line default
            #line hidden
WriteLiteral("            ");

WriteLiteral(" matching \'");

            
            #line 9 "..\..\Views\Teams.cshtml"
                    Write(Model.TeamFilter.Query);

            
            #line default
            #line hidden
WriteLiteral("\'\r\n");

            
            #line 10 "..\..\Views\Teams.cshtml"
        }

            
            #line default
            #line hidden
WriteLiteral("    </h1>\r\n\r\n    <ul");

WriteLiteral(" class=\"nav nav-tabs nav-tabs-has-add\"");

WriteLiteral(">\r\n        <li");

WriteLiteral(" class=\"nav-item\"");

WriteLiteral(">\r\n            <em");

WriteLiteral(" class=\"nav-link active\"");

WriteLiteral(">Search</em>\r\n        </li>\r\n        <li");

WriteLiteral(" class=\"nav-item\"");

WriteLiteral(">\r\n            <a");

WriteLiteral(" class=\"nav-link\"");

WriteAttribute("href", Tuple.Create(" href=\"", 557), Tuple.Create("\"", 603)
            
            #line 18 "..\..\Views\Teams.cshtml"
, Tuple.Create(Tuple.Create("", 564), Tuple.Create<System.Object, System.Int32>(Stoolball.Constants.Pages.TeamsUrl
            
            #line default
            #line hidden
, 564), false)
, Tuple.Create(Tuple.Create("", 599), Tuple.Create("/map", 599), true)
);

WriteLiteral(">Map</a>\r\n        </li>\r\n        <li");

WriteLiteral(" class=\"nav-item nav-item-admin nav-item-admin-sm-last d-md-none\"");

WriteLiteral(">\r\n            <div");

WriteLiteral(" class=\"dropdown\"");

WriteLiteral(">\r\n                <button");

WriteLiteral(" class=\"btn nav-link nav-link-add\"");

WriteLiteral(" type=\"button\"");

WriteLiteral(" id=\"add-menu__button\"");

WriteLiteral(" data-toggle=\"dropdown\"");

WriteLiteral(" aria-haspopup=\"true\"");

WriteLiteral(" aria-expanded=\"false\"");

WriteLiteral(">\r\n                    Add club or team\r\n                </button>\r\n             " +
"   <ul");

WriteLiteral(" class=\"dropdown-menu dropdown-menu-right\"");

WriteLiteral(" aria-labelledby=\"add-menu__button\"");

WriteLiteral(">\r\n                    <li");

WriteLiteral(" class=\"dropdown-item\"");

WriteLiteral("><a");

WriteLiteral(" href=\"/clubs/add\"");

WriteLiteral(">Add club</a></li>\r\n                    <li");

WriteLiteral(" class=\"dropdown-item\"");

WriteLiteral("><a");

WriteLiteral(" href=\"/teams/add\"");

WriteLiteral(">Add team</a></li>\r\n                </ul>\r\n            </div>\r\n        </li>\r\n   " +
"     <li");

WriteLiteral(" class=\"nav-item nav-item-admin d-none d-md-flex nav-item-admin-md-first\"");

WriteLiteral(">\r\n            <a");

WriteLiteral(" class=\"nav-link nav-link-add\"");

WriteLiteral(" href=\"/clubs/add\"");

WriteLiteral(">Add club</a>\r\n        </li>\r\n        <li");

WriteLiteral(" class=\"nav-item nav-item-admin d-none d-md-flex\"");

WriteLiteral(">\r\n            <a");

WriteLiteral(" class=\"nav-link nav-link-add\"");

WriteLiteral(" href=\"/teams/add\"");

WriteLiteral(">Add team</a>\r\n        </li>\r\n    </ul>\r\n\r\n");

            
            #line 39 "..\..\Views\Teams.cshtml"
    
            
            #line default
            #line hidden
            
            #line 39 "..\..\Views\Teams.cshtml"
     if (string.IsNullOrEmpty(Model.TeamFilter.Query))
    {

            
            #line default
            #line hidden
WriteLiteral("        <p");

WriteLiteral(" class=\"alert alert-info alert-tip\"");

WriteLiteral(">Try searching for <a");

WriteLiteral(" href=\"/teams?q=ladies\"");

WriteLiteral(">ladies teams</a>, <a");

WriteLiteral(" href=\"/teams?q=mixed\"");

WriteLiteral(">mixed teams</a> or <a");

WriteLiteral(" href=\"/teams?q=junior\"");

WriteLiteral(">junior teams</a>.</p>\r\n");

            
            #line 42 "..\..\Views\Teams.cshtml"
    }

            
            #line default
            #line hidden
WriteLiteral("    <form");

WriteLiteral(" method=\"get\"");

WriteAttribute("action", Tuple.Create(" action=\"", 1940), Tuple.Create("\"", 1961)
            
            #line 43 "..\..\Views\Teams.cshtml"
, Tuple.Create(Tuple.Create("", 1949), Tuple.Create<System.Object, System.Int32>(Request.Url
            
            #line default
            #line hidden
, 1949), false)
);

WriteLiteral(" class=\"form-inline form-search\"");

WriteLiteral(">\r\n        <label");

WriteLiteral(" class=\"sr-only\"");

WriteLiteral(" for=\"team-search\"");

WriteLiteral(">Team name</label>\r\n        <input");

WriteLiteral(" type=\"search\"");

WriteLiteral(" class=\"form-control\"");

WriteLiteral(" id=\"team-search\"");

WriteLiteral(" name=\"q\"");

WriteAttribute("value", Tuple.Create(" value=\"", 2140), Tuple.Create("\"", 2171)
            
            #line 45 "..\..\Views\Teams.cshtml"
   , Tuple.Create(Tuple.Create("", 2148), Tuple.Create<System.Object, System.Int32>(Model.TeamFilter.Query
            
            #line default
            #line hidden
, 2148), false)
);

WriteLiteral(" />\r\n        <button");

WriteLiteral(" type=\"submit\"");

WriteLiteral(" class=\"btn btn-primary\"");

WriteLiteral(">Search</button>\r\n    </form>\r\n\r\n    <dl>\r\n");

            
            #line 50 "..\..\Views\Teams.cshtml"
        
            
            #line default
            #line hidden
            
            #line 50 "..\..\Views\Teams.cshtml"
         foreach (var listing in Model.Teams)
        {
            var linkText = listing.ClubOrTeamName;
            var location = listing.MatchLocations.FirstOrDefault()?.LocalityOrTown();
            if (!string.IsNullOrEmpty(location) &&
                !linkText.Replace("'", string.Empty).ToUpperInvariant().Contains(location.Replace("'", string.Empty).ToUpperInvariant()))
            {
                linkText += ", " + location;
            }

            listing.PlayerTypes.Sort(); // by id, which puts adult teams before junior
            var playerTypes = $"{listing.PlayerTypes.Select((value, index) => value.Humanize(index > 0 ? LetterCasing.LowerCase : LetterCasing.Sentence)).Humanize()}";

            var detail = string.Empty;
            if (listing.TeamType.HasValue)
            {
                if (listing.TeamType == TeamType.SchoolClub)
                {
                    detail = playerTypes + " school club. ";
                }
                else
                {
                    detail = listing.TeamType.Humanize(LetterCasing.Sentence) + $" {playerTypes.Humanize(LetterCasing.LowerCase)} team. ";
                }
                if (!listing.Active)
                {
                    detail += $"No longer active.";
                }
            }
            else
            {
                if (listing.PlayerTypes.Count > 0)
                {
                    detail = $"Club with {(listing.PlayerTypes.Count > 1 ? string.Empty : "one ")}{playerTypes.Humanize(LetterCasing.LowerCase)} {(listing.PlayerTypes.Count > 1 ? "teams" : "team")}. ";
                    if (!listing.Active)
                    {
                        detail += $"No longer active.";
                    }
                }
                else
                {
                    detail = "Club with no active teams. ";
                }
            }

            
            #line default
            #line hidden
WriteLiteral("            <dt");

WriteLiteral(" class=\"list-results__title\"");

WriteLiteral("><a");

WriteAttribute("href", Tuple.Create(" href=\"", 4256), Tuple.Create("\"", 4287)
            
            #line 94 "..\..\Views\Teams.cshtml"
, Tuple.Create(Tuple.Create("", 4263), Tuple.Create<System.Object, System.Int32>(listing.ClubOrTeamRoute
            
            #line default
            #line hidden
, 4263), false)
);

WriteLiteral(">");

            
            #line 94 "..\..\Views\Teams.cshtml"
                                                                          Write(linkText);

            
            #line default
            #line hidden
WriteLiteral("</a></dt>\r\n");

WriteLiteral("            <dd");

WriteLiteral(" class=\"list-results__detail\"");

WriteLiteral(">");

            
            #line 95 "..\..\Views\Teams.cshtml"
                                        Write(detail);

            
            #line default
            #line hidden
WriteLiteral("</dd>\r\n");

            
            #line 96 "..\..\Views\Teams.cshtml"
        }

            
            #line default
            #line hidden
WriteLiteral("    </dl>\r\n");

            
            #line 98 "..\..\Views\Teams.cshtml"
    
            
            #line default
            #line hidden
            
            #line 98 "..\..\Views\Teams.cshtml"
     if (Model.TotalTeams > (Model.TeamFilter.PageNumber * Model.TeamFilter.PageSize))
    {
        var query = HttpUtility.ParseQueryString(Request.Url.Query);
        query["page"] = (Model.TeamFilter.PageNumber + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);

            
            #line default
            #line hidden
WriteLiteral("        <p><a");

WriteAttribute("href", Tuple.Create(" href=\"", 4688), Tuple.Create("\"", 4708)
, Tuple.Create(Tuple.Create("", 4695), Tuple.Create("/teams?", 4695), true)
            
            #line 102 "..\..\Views\Teams.cshtml"
, Tuple.Create(Tuple.Create("", 4702), Tuple.Create<System.Object, System.Int32>(query
            
            #line default
            #line hidden
, 4702), false)
);

WriteLiteral(" class=\"btn btn-secondary\"");

WriteLiteral(">Next page</a></p>\r\n");

            
            #line 103 "..\..\Views\Teams.cshtml"
    }

            
            #line default
            #line hidden
WriteLiteral("</div>");

        }
    }
}
#pragma warning restore 1591