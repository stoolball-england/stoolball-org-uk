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
    
    #line 2 "..\..\Views\CreateLeagueMatch.cshtml"
    using ClientDependency.Core.Mvc;
    
    #line default
    #line hidden
    using Examine;
    
    #line 4 "..\..\Views\CreateLeagueMatch.cshtml"
    using Stoolball.Security;
    
    #line default
    #line hidden
    
    #line 3 "..\..\Views\CreateLeagueMatch.cshtml"
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
    [System.Web.WebPages.PageVirtualPathAttribute("~/Views/CreateLeagueMatch.cshtml")]
    public partial class _Views_CreateLeagueMatch_cshtml : Umbraco.Web.Mvc.UmbracoViewPage<EditLeagueMatchViewModel>
    {
        public _Views_CreateLeagueMatch_cshtml()
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

            
            #line 8 "..\..\Views\CreateLeagueMatch.cshtml"
  
    Html.EnableClientValidation();
    Html.EnableUnobtrusiveJavaScript();
    Html.RequiresJs("~/scripts/jquery.validate.min.js");
    Html.RequiresJs("~/scripts/jquery.validate.unobtrusive.min.js");

    Html.RequiresJs("~/js/libs/jquery.autocomplete.min.js", 50);
    Html.RequiresCss("~/css/autocomplete.min.css");

    Html.RequiresCss("~/css/related-items.min.css");
    Html.RequiresJs("~/js/related-item.js");

    Html.RequiresJs("/umbraco/lib/tinymce/tinymce.min.js", 90);
    Html.RequiresJs("/js/tinymce.js");

    var h1 = string.Empty;
    if (Model.Team != null)
    {
        h1 = $"Add a league match for {Model.Team.TeamName}";
    }
    else if (Model.Season != null)
    {
        h1 = $"Add a league match in the {Model.Season.SeasonFullName()}";
    }

            
            #line default
            #line hidden
WriteLiteral("\r\n<div");

WriteLiteral(" class=\"container-xl\"");

WriteLiteral(">\r\n    <h1>");

            
            #line 34 "..\..\Views\CreateLeagueMatch.cshtml"
   Write(h1);

            
            #line default
            #line hidden
WriteLiteral("</h1>\r\n\r\n");

            
            #line 36 "..\..\Views\CreateLeagueMatch.cshtml"
    
            
            #line default
            #line hidden
            
            #line 36 "..\..\Views\CreateLeagueMatch.cshtml"
     if (Model.IsAuthorized[AuthorizedAction.CreateMatch])
    {
        if (Model.Season == null || Model.Season.Teams.Count > 1)
        {
            using (Html.BeginUmbracoForm<CreateLeagueMatchSurfaceController>
                ("CreateMatch"))
            {
            
            
            #line default
            #line hidden
            
            #line 43 "..\..\Views\CreateLeagueMatch.cshtml"
       Write(Html.Partial("_CreateOrEditLeagueMatch"));

            
            #line default
            #line hidden
            
            #line 43 "..\..\Views\CreateLeagueMatch.cshtml"
                                                     

            
            #line default
            #line hidden
WriteLiteral("            <button");

WriteLiteral(" class=\"btn btn-primary\"");

WriteLiteral(">Save match</button>\r\n");

            
            #line 45 "..\..\Views\CreateLeagueMatch.cshtml"
            }
        }
        else
        {

            
            #line default
            #line hidden
WriteLiteral("            <p>You need at least two teams in the ");

            
            #line 49 "..\..\Views\CreateLeagueMatch.cshtml"
                                             Write(Model.Season.SeasonFullName());

            
            #line default
            #line hidden
WriteLiteral(" to add a league match.</p>\r\n");

            
            #line 50 "..\..\Views\CreateLeagueMatch.cshtml"
            if ( Model.IsAuthorized[AuthorizedAction.EditCompetition])
            {

            
            #line default
            #line hidden
WriteLiteral("                <p><a");

WriteAttribute("href", Tuple.Create(" href=\"", 1745), Tuple.Create("\"", 1788)
            
            #line 52 "..\..\Views\CreateLeagueMatch.cshtml"
, Tuple.Create(Tuple.Create("", 1752), Tuple.Create<System.Object, System.Int32>(Model.Season.SeasonRoute
            
            #line default
            #line hidden
, 1752), false)
, Tuple.Create(Tuple.Create("", 1777), Tuple.Create("/edit/teams", 1777), true)
);

WriteLiteral(" class=\"btn btn-secondary\"");

WriteLiteral(">Edit teams</a></p>\r\n");

            
            #line 53 "..\..\Views\CreateLeagueMatch.cshtml"
            }
            else
            {

            
            #line default
            #line hidden
WriteLiteral("                <p><a");

WriteAttribute("href", Tuple.Create(" href=\"", 1905), Tuple.Create("\"", 1954)
            
            #line 56 "..\..\Views\CreateLeagueMatch.cshtml"
, Tuple.Create(Tuple.Create("", 1912), Tuple.Create<System.Object, System.Int32>(Model.Season.Competition.CompetitionRoute
            
            #line default
            #line hidden
, 1912), false)
);

WriteLiteral(">Contact the administrators of the ");

            
            #line 56 "..\..\Views\CreateLeagueMatch.cshtml"
                                                                                                     Write(Model.Season.Competition.CompetitionName);

            
            #line default
            #line hidden
WriteLiteral("</a> \r\n                    and ask them to add the teams playing in the ");

            
            #line 57 "..\..\Views\CreateLeagueMatch.cshtml"
                                                            Write(Model.Season.SeasonName());

            
            #line default
            #line hidden
WriteLiteral(", so that you can add a match.</p>\r\n");

            
            #line 58 "..\..\Views\CreateLeagueMatch.cshtml"
            }
        }
    }
    else
    {
        
            
            #line default
            #line hidden
            
            #line 63 "..\..\Views\CreateLeagueMatch.cshtml"
   Write(Html.Partial("_Login"));

            
            #line default
            #line hidden
            
            #line 63 "..\..\Views\CreateLeagueMatch.cshtml"
                               
    }

            
            #line default
            #line hidden
WriteLiteral("</div>");

        }
    }
}
#pragma warning restore 1591
