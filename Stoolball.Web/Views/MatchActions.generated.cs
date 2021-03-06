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
    
    #line 2 "..\..\Views\MatchActions.cshtml"
    using Stoolball.Matches;
    
    #line default
    #line hidden
    
    #line 4 "..\..\Views\MatchActions.cshtml"
    using Stoolball.Security;
    
    #line default
    #line hidden
    
    #line 3 "..\..\Views\MatchActions.cshtml"
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
    [System.Web.WebPages.PageVirtualPathAttribute("~/Views/MatchActions.cshtml")]
    public partial class _Views_MatchActions_cshtml : Umbraco.Web.Mvc.UmbracoViewPage<MatchViewModel>
    {
        public _Views_MatchActions_cshtml()
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

WriteLiteral(">\r\n    <h1>Edit ");

            
            #line 9 "..\..\Views\MatchActions.cshtml"
        Write(Html.MatchFullName(Model.Match, x => Model.DateTimeFormatter.FormatDate(x, false)));

            
            #line default
            #line hidden
WriteLiteral("</h1>\r\n\r\n");

            
            #line 11 "..\..\Views\MatchActions.cshtml"
    
            
            #line default
            #line hidden
            
            #line 11 "..\..\Views\MatchActions.cshtml"
     if (Model.IsAuthorized[AuthorizedAction.EditMatch] || Model.IsAuthorized[AuthorizedAction.EditMatchResult] || Model.IsAuthorized[AuthorizedAction.DeleteMatch])
    {

            
            #line default
            #line hidden
WriteLiteral("        <dl>\r\n");

            
            #line 14 "..\..\Views\MatchActions.cshtml"
            
            
            #line default
            #line hidden
            
            #line 14 "..\..\Views\MatchActions.cshtml"
             if (Model.IsAuthorized[AuthorizedAction.EditMatch] && Model.Match.StartTime > DateTime.UtcNow && Model.Match.Tournament == null)
            {
                switch (Model.Match.MatchType)
                {
                    case MatchType.FriendlyMatch:

            
            #line default
            #line hidden
WriteLiteral("                        <dt><a");

WriteAttribute("href", Tuple.Create(" href=\"", 841), Tuple.Create("\"", 885)
            
            #line 19 "..\..\Views\MatchActions.cshtml"
, Tuple.Create(Tuple.Create("", 848), Tuple.Create<System.Object, System.Int32>(Model.Match.MatchRoute
            
            #line default
            #line hidden
, 848), false)
, Tuple.Create(Tuple.Create("", 871), Tuple.Create("/edit/friendly", 871), true)
);

WriteLiteral(">Edit ");

            
            #line 19 "..\..\Views\MatchActions.cshtml"
                                                                            Write(Html.MatchFullName(Model.Match, x => Model.DateTimeFormatter.FormatDate(x, false)));

            
            #line default
            #line hidden
WriteLiteral("</a></dt>\r\n");

            
            #line 20 "..\..\Views\MatchActions.cshtml"
                        break;
                    case MatchType.LeagueMatch:

            
            #line default
            #line hidden
WriteLiteral("                        <dt><a");

WriteAttribute("href", Tuple.Create(" href=\"", 1097), Tuple.Create("\"", 1139)
            
            #line 22 "..\..\Views\MatchActions.cshtml"
, Tuple.Create(Tuple.Create("", 1104), Tuple.Create<System.Object, System.Int32>(Model.Match.MatchRoute
            
            #line default
            #line hidden
, 1104), false)
, Tuple.Create(Tuple.Create("", 1127), Tuple.Create("/edit/league", 1127), true)
);

WriteLiteral(">Edit ");

            
            #line 22 "..\..\Views\MatchActions.cshtml"
                                                                          Write(Html.MatchFullName(Model.Match, x => Model.DateTimeFormatter.FormatDate(x, false)));

            
            #line default
            #line hidden
WriteLiteral("</a></dt>\r\n");

            
            #line 23 "..\..\Views\MatchActions.cshtml"
                        break;
                    case MatchType.KnockoutMatch:

            
            #line default
            #line hidden
WriteLiteral("                        <dt><a");

WriteAttribute("href", Tuple.Create(" href=\"", 1353), Tuple.Create("\"", 1397)
            
            #line 25 "..\..\Views\MatchActions.cshtml"
, Tuple.Create(Tuple.Create("", 1360), Tuple.Create<System.Object, System.Int32>(Model.Match.MatchRoute
            
            #line default
            #line hidden
, 1360), false)
, Tuple.Create(Tuple.Create("", 1383), Tuple.Create("/edit/knockout", 1383), true)
);

WriteLiteral(">Edit ");

            
            #line 25 "..\..\Views\MatchActions.cshtml"
                                                                            Write(Html.MatchFullName(Model.Match, x => Model.DateTimeFormatter.FormatDate(x, false)));

            
            #line default
            #line hidden
WriteLiteral("</a></dt>\r\n");

            
            #line 26 "..\..\Views\MatchActions.cshtml"
                        break;
                    case MatchType.TrainingSession:

            
            #line default
            #line hidden
WriteLiteral("                        <dt><a");

WriteAttribute("href", Tuple.Create(" href=\"", 1613), Tuple.Create("\"", 1657)
            
            #line 28 "..\..\Views\MatchActions.cshtml"
, Tuple.Create(Tuple.Create("", 1620), Tuple.Create<System.Object, System.Int32>(Model.Match.MatchRoute
            
            #line default
            #line hidden
, 1620), false)
, Tuple.Create(Tuple.Create("", 1643), Tuple.Create("/edit/training", 1643), true)
);

WriteLiteral(">Edit ");

            
            #line 28 "..\..\Views\MatchActions.cshtml"
                                                                            Write(Html.MatchFullName(Model.Match, x => Model.DateTimeFormatter.FormatDate(x, false)));

            
            #line default
            #line hidden
WriteLiteral("</a></dt>\r\n");

            
            #line 29 "..\..\Views\MatchActions.cshtml"
                        break;
                }

            
            #line default
            #line hidden
WriteLiteral("                <dd>Set the date and time, teams, location and notes.</dd>\r\n");

            
            #line 32 "..\..\Views\MatchActions.cshtml"
            }
            else if (Model.IsAuthorized[AuthorizedAction.EditMatchResult] && Model.Match.StartTime <= DateTime.UtcNow && Model.Match.Tournament == null)
            {
                if (Model.Match.MatchType == MatchType.LeagueMatch || Model.Match.MatchType == MatchType.KnockoutMatch || Model.Match.MatchType == MatchType.FriendlyMatch)
                {

            
            #line default
            #line hidden
WriteLiteral("                    <dt><a");

WriteAttribute("href", Tuple.Create(" href=\"", 2287), Tuple.Create("\"", 2336)
            
            #line 37 "..\..\Views\MatchActions.cshtml"
, Tuple.Create(Tuple.Create("", 2294), Tuple.Create<System.Object, System.Int32>(Model.Match.MatchRoute
            
            #line default
            #line hidden
, 2294), false)
, Tuple.Create(Tuple.Create("", 2317), Tuple.Create("/edit/start-of-play", 2317), true)
);

WriteLiteral(">Edit the result of ");

            
            #line 37 "..\..\Views\MatchActions.cshtml"
                                                                                           Write(Html.MatchFullName(Model.Match, x => Model.DateTimeFormatter.FormatDate(x, false)));

            
            #line default
            #line hidden
WriteLiteral("</a></dt>\r\n");

WriteLiteral("                    <dd>Enter scorecards, set who won the toss and their decision" +
", or say why the match didn\'t happen.</dd>\r\n");

            
            #line 39 "..\..\Views\MatchActions.cshtml"
                }
            }

            
            #line default
            #line hidden
WriteLiteral("            ");

            
            #line 41 "..\..\Views\MatchActions.cshtml"
             if (Model.IsAuthorized[AuthorizedAction.DeleteMatch])
            {

            
            #line default
            #line hidden
WriteLiteral("                <dt><a");

WriteAttribute("href", Tuple.Create(" href=\"", 2715), Tuple.Create("\"", 2752)
            
            #line 43 "..\..\Views\MatchActions.cshtml"
, Tuple.Create(Tuple.Create("", 2722), Tuple.Create<System.Object, System.Int32>(Model.Match.MatchRoute
            
            #line default
            #line hidden
, 2722), false)
, Tuple.Create(Tuple.Create("", 2745), Tuple.Create("/delete", 2745), true)
);

WriteLiteral(">Delete ");

            
            #line 43 "..\..\Views\MatchActions.cshtml"
                                                               Write(Html.MatchFullName(Model.Match, x => Model.DateTimeFormatter.FormatDate(x, false)));

            
            #line default
            #line hidden
WriteLiteral("</a></dt>\r\n");

WriteLiteral("                <dd>Match statistics will be lost. Competition results may be cha" +
"nged. Players that only feature in this match will be deleted.</dd>\r\n");

            
            #line 45 "..\..\Views\MatchActions.cshtml"
            }

            
            #line default
            #line hidden
WriteLiteral("        </dl>\r\n");

            
            #line 47 "..\..\Views\MatchActions.cshtml"
    }
    else
    {
        
            
            #line default
            #line hidden
            
            #line 50 "..\..\Views\MatchActions.cshtml"
   Write(Html.Partial("_Login"));

            
            #line default
            #line hidden
            
            #line 50 "..\..\Views\MatchActions.cshtml"
                               
    }

            
            #line default
            #line hidden
WriteLiteral("</div>");

        }
    }
}
#pragma warning restore 1591
