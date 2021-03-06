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
    
    #line 2 "..\..\Views\SeasonStatistics.cshtml"
    using ClientDependency.Core.Mvc;
    
    #line default
    #line hidden
    using Examine;
    
    #line 4 "..\..\Views\SeasonStatistics.cshtml"
    using Stoolball.Competitions;
    
    #line default
    #line hidden
    
    #line 5 "..\..\Views\SeasonStatistics.cshtml"
    using Stoolball.Matches;
    
    #line default
    #line hidden
    
    #line 6 "..\..\Views\SeasonStatistics.cshtml"
    using Stoolball.Statistics;
    
    #line default
    #line hidden
    
    #line 3 "..\..\Views\SeasonStatistics.cshtml"
    using Stoolball.Web.Statistics;
    
    #line default
    #line hidden
    using Umbraco.Core;
    using Umbraco.Core.Models;
    using Umbraco.Core.Models.PublishedContent;
    using Umbraco.Web;
    using Umbraco.Web.Mvc;
    using Umbraco.Web.PublishedModels;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Views/SeasonStatistics.cshtml")]
    public partial class _Views_SeasonStatistics_cshtml : Umbraco.Web.Mvc.UmbracoViewPage<StatisticsSummaryViewModel<Season>>
    {
        public _Views_SeasonStatistics_cshtml()
        {
        }
        public override void Execute()
        {
            
            #line 7 "..\..\Views\SeasonStatistics.cshtml"
  
    Html.RequiresCss("/statistics/statistics.min.css");

    var individualScores = new StatisticsViewModel<PlayerInnings>(Model, Services.UserService) { ShowCaption = true, ShowPlayerColumn = true, StatisticsFilter = Model.StatisticsFilter };
    individualScores.Results.AddRange(Model.PlayerInnings);
    var bowlingFigures = new StatisticsViewModel<BowlingFigures>(Model, Services.UserService) { ShowCaption = true, ShowPlayerColumn = true, StatisticsFilter = Model.StatisticsFilter };
    bowlingFigures.Results.AddRange(Model.BowlingFigures);
    var mostRuns = new StatisticsViewModel<BestStatistic>(Model, Services.UserService) { ShowCaption = true, ShowTeamsColumn = true, StatisticsFilter = Model.StatisticsFilter };
    mostRuns.Results.AddRange(Model.MostRuns);
    var mostWickets = new StatisticsViewModel<BestStatistic>(Model, Services.UserService) { ShowCaption = true, ShowTeamsColumn = true, StatisticsFilter = Model.StatisticsFilter };
    mostWickets.Results.AddRange(Model.MostWickets);
    var mostCatches = new StatisticsViewModel<BestStatistic>(Model, Services.UserService) { ShowCaption = true, ShowTeamsColumn = true, StatisticsFilter = Model.StatisticsFilter };
    mostCatches.Results.AddRange(Model.MostCatches);

            
            #line default
            #line hidden
WriteLiteral("\r\n<div");

WriteLiteral(" class=\"container-xl\"");

WriteLiteral(">\r\n    <h1>");

            
            #line 22 "..\..\Views\SeasonStatistics.cshtml"
   Write(Model.Context.SeasonFullNameAndPlayerType());

            
            #line default
            #line hidden
WriteLiteral("</h1>\r\n\r\n");

            
            #line 24 "..\..\Views\SeasonStatistics.cshtml"
    
            
            #line default
            #line hidden
            
            #line 24 "..\..\Views\SeasonStatistics.cshtml"
     if (Model.Context.Competition.UntilYear.HasValue)
    {

            
            #line default
            #line hidden
WriteLiteral("        <div");

WriteLiteral(" class=\"alert alert-info\"");

WriteLiteral(">\r\n            <p><strong>This competition isn\'t played any more.</strong></p>\r\n " +
"       </div>\r\n");

            
            #line 29 "..\..\Views\SeasonStatistics.cshtml"
    }

            
            #line default
            #line hidden
WriteLiteral("\r\n    <ul");

WriteLiteral(" class=\"nav nav-tabs\"");

WriteLiteral(">\r\n        <li");

WriteLiteral(" class=\"nav-item\"");

WriteLiteral(">\r\n            <a");

WriteLiteral(" class=\"nav-link\"");

WriteAttribute("href", Tuple.Create(" href=\"", 1880), Tuple.Create("\"", 1913)
            
            #line 33 "..\..\Views\SeasonStatistics.cshtml"
, Tuple.Create(Tuple.Create("", 1887), Tuple.Create<System.Object, System.Int32>(Model.Context.SeasonRoute
            
            #line default
            #line hidden
, 1887), false)
);

WriteLiteral(">Summary</a>\r\n        </li>\r\n        <li");

WriteLiteral(" class=\"nav-item\"");

WriteLiteral(">\r\n            <a");

WriteLiteral(" class=\"nav-link\"");

WriteAttribute("href", Tuple.Create(" href=\"", 2005), Tuple.Create("\"", 2046)
            
            #line 36 "..\..\Views\SeasonStatistics.cshtml"
, Tuple.Create(Tuple.Create("", 2012), Tuple.Create<System.Object, System.Int32>(Model.Context.SeasonRoute
            
            #line default
            #line hidden
, 2012), false)
, Tuple.Create(Tuple.Create("", 2038), Tuple.Create("/matches", 2038), true)
);

WriteLiteral(">Matches</a>\r\n        </li>\r\n        <li");

WriteLiteral(" class=\"nav-item\"");

WriteLiteral(">\r\n            <a");

WriteLiteral(" class=\"nav-link\"");

WriteAttribute("href", Tuple.Create(" href=\"", 2138), Tuple.Create("\"", 2175)
            
            #line 39 "..\..\Views\SeasonStatistics.cshtml"
, Tuple.Create(Tuple.Create("", 2145), Tuple.Create<System.Object, System.Int32>(Model.Context.SeasonRoute
            
            #line default
            #line hidden
, 2145), false)
, Tuple.Create(Tuple.Create("", 2171), Tuple.Create("/map", 2171), true)
);

WriteLiteral(">Map</a>\r\n        </li>\r\n");

            
            #line 41 "..\..\Views\SeasonStatistics.cshtml"
        
            
            #line default
            #line hidden
            
            #line 41 "..\..\Views\SeasonStatistics.cshtml"
         if (Model.Context.MatchTypes.Contains(MatchType.LeagueMatch) ||
            Model.Context.MatchTypes.Contains(MatchType.KnockoutMatch) ||
            Model.Context.MatchTypes.Contains(MatchType.FriendlyMatch) ||
            !string.IsNullOrEmpty(Model.Context.Results))
        {

            
            #line default
            #line hidden
WriteLiteral("            <li");

WriteLiteral(" class=\"nav-item\"");

WriteLiteral(">\r\n                <a");

WriteLiteral(" class=\"nav-link\"");

WriteAttribute("href", Tuple.Create(" href=\"", 2565), Tuple.Create("\"", 2604)
            
            #line 47 "..\..\Views\SeasonStatistics.cshtml"
, Tuple.Create(Tuple.Create("", 2572), Tuple.Create<System.Object, System.Int32>(Model.Context.SeasonRoute
            
            #line default
            #line hidden
, 2572), false)
, Tuple.Create(Tuple.Create("", 2598), Tuple.Create("/table", 2598), true)
);

WriteLiteral(">Table</a>\r\n            </li>\r\n");

            
            #line 49 "..\..\Views\SeasonStatistics.cshtml"
        }

            
            #line default
            #line hidden
WriteLiteral("        <li");

WriteLiteral(" class=\"nav-item\"");

WriteLiteral(">\r\n            <em");

WriteLiteral(" class=\"nav-link active\"");

WriteLiteral(">Statistics</em>\r\n        </li>\r\n    </ul>\r\n\r\n");

            
            #line 55 "..\..\Views\SeasonStatistics.cshtml"
    
            
            #line default
            #line hidden
            
            #line 55 "..\..\Views\SeasonStatistics.cshtml"
     if (!Model.PlayerInnings.Any() && !Model.BowlingFigures.Any() && !Model.MostRuns.Any() && !Model.MostWickets.Any() && !Model.MostCatches.Any())
    {
        
            
            #line default
            #line hidden
            
            #line 57 "..\..\Views\SeasonStatistics.cshtml"
   Write(Html.Partial("_NoData"));

            
            #line default
            #line hidden
            
            #line 57 "..\..\Views\SeasonStatistics.cshtml"
                                
    }
    else
    {
        
            
            #line default
            #line hidden
            
            #line 61 "..\..\Views\SeasonStatistics.cshtml"
   Write(Html.Partial("_IndividualScores", individualScores));

            
            #line default
            #line hidden
            
            #line 61 "..\..\Views\SeasonStatistics.cshtml"
                                                            
        
            
            #line default
            #line hidden
            
            #line 62 "..\..\Views\SeasonStatistics.cshtml"
   Write(Html.Partial("_MostRuns", mostRuns));

            
            #line default
            #line hidden
            
            #line 62 "..\..\Views\SeasonStatistics.cshtml"
                                            
        
            
            #line default
            #line hidden
            
            #line 63 "..\..\Views\SeasonStatistics.cshtml"
   Write(Html.Partial("_BattingAverage", new StatisticsViewModel<BestStatistic>(Model, Services.UserService) { ShowLinkOnly = true }));

            
            #line default
            #line hidden
            
            #line 63 "..\..\Views\SeasonStatistics.cshtml"
                                                                                                                                     
        
            
            #line default
            #line hidden
            
            #line 64 "..\..\Views\SeasonStatistics.cshtml"
   Write(Html.Partial("_BattingStrikeRate", new StatisticsViewModel<BestStatistic>(Model, Services.UserService) { ShowLinkOnly = true }));

            
            #line default
            #line hidden
            
            #line 64 "..\..\Views\SeasonStatistics.cshtml"
                                                                                                                                        
        
            
            #line default
            #line hidden
            
            #line 65 "..\..\Views\SeasonStatistics.cshtml"
   Write(Html.Partial("_BowlingFigures", bowlingFigures));

            
            #line default
            #line hidden
            
            #line 65 "..\..\Views\SeasonStatistics.cshtml"
                                                        
        
            
            #line default
            #line hidden
            
            #line 66 "..\..\Views\SeasonStatistics.cshtml"
   Write(Html.Partial("_MostWickets", mostWickets));

            
            #line default
            #line hidden
            
            #line 66 "..\..\Views\SeasonStatistics.cshtml"
                                                  
        
            
            #line default
            #line hidden
            
            #line 67 "..\..\Views\SeasonStatistics.cshtml"
   Write(Html.Partial("_BowlingAverage", new StatisticsViewModel<BestStatistic>(Model, Services.UserService) { ShowLinkOnly = true }));

            
            #line default
            #line hidden
            
            #line 67 "..\..\Views\SeasonStatistics.cshtml"
                                                                                                                                     
        
            
            #line default
            #line hidden
            
            #line 68 "..\..\Views\SeasonStatistics.cshtml"
   Write(Html.Partial("_EconomyRate", new StatisticsViewModel<BestStatistic>(Model, Services.UserService) { ShowLinkOnly = true }));

            
            #line default
            #line hidden
            
            #line 68 "..\..\Views\SeasonStatistics.cshtml"
                                                                                                                                  
        
            
            #line default
            #line hidden
            
            #line 69 "..\..\Views\SeasonStatistics.cshtml"
   Write(Html.Partial("_BowlingStrikeRate", new StatisticsViewModel<BestStatistic>(Model, Services.UserService) { ShowLinkOnly = true }));

            
            #line default
            #line hidden
            
            #line 69 "..\..\Views\SeasonStatistics.cshtml"
                                                                                                                                        
        
            
            #line default
            #line hidden
            
            #line 70 "..\..\Views\SeasonStatistics.cshtml"
   Write(Html.Partial("_MostCatches", mostCatches));

            
            #line default
            #line hidden
            
            #line 70 "..\..\Views\SeasonStatistics.cshtml"
                                                  
        
            
            #line default
            #line hidden
            
            #line 71 "..\..\Views\SeasonStatistics.cshtml"
   Write(Html.Partial("_MostRunOuts", new StatisticsViewModel<BestStatistic>(Model, Services.UserService) { ShowLinkOnly = true }));

            
            #line default
            #line hidden
            
            #line 71 "..\..\Views\SeasonStatistics.cshtml"
                                                                                                                                  
    }

            
            #line default
            #line hidden
WriteLiteral("    ");

            
            #line 73 "..\..\Views\SeasonStatistics.cshtml"
     if (Model.Context.Competition.Seasons.Count > 1)
    {
        var previous = Model.Context.Competition.Seasons.FirstOrDefault(x => x.FromYear <= Model.Context.FromYear && x.UntilYear <= Model.Context.UntilYear && x.SeasonId != Model.Context.SeasonId);
        var next = Model.Context.Competition.Seasons.LastOrDefault(x => x.FromYear >= Model.Context.FromYear && x.UntilYear >= Model.Context.UntilYear && x.SeasonId != Model.Context.SeasonId);

            
            #line default
            #line hidden
WriteLiteral("        <h2");

WriteLiteral(" class=\"sr-only\"");

WriteLiteral(">Seasons in this competition</h2>\r\n");

WriteLiteral("        <p");

WriteLiteral(" class=\"d-print-none\"");

WriteLiteral(">\r\n");

            
            #line 79 "..\..\Views\SeasonStatistics.cshtml"
            
            
            #line default
            #line hidden
            
            #line 79 "..\..\Views\SeasonStatistics.cshtml"
             if (previous != null)
            {

            
            #line default
            #line hidden
WriteLiteral("                <a");

WriteAttribute("href", Tuple.Create(" href=\"", 4685), Tuple.Create("\"", 4724)
            
            #line 81 "..\..\Views\SeasonStatistics.cshtml"
, Tuple.Create(Tuple.Create("", 4692), Tuple.Create<System.Object, System.Int32>(previous.SeasonRoute
            
            #line default
            #line hidden
, 4692), false)
, Tuple.Create(Tuple.Create("", 4713), Tuple.Create("/statistics", 4713), true)
);

WriteLiteral(" class=\"btn btn-secondary btn-back\"");

WriteLiteral(">Previous season</a>\r\n");

            
            #line 82 "..\..\Views\SeasonStatistics.cshtml"
            }

            
            #line default
            #line hidden
WriteLiteral("            ");

            
            #line 83 "..\..\Views\SeasonStatistics.cshtml"
             if (next != null)
            {

            
            #line default
            #line hidden
WriteLiteral("                <a");

WriteAttribute("href", Tuple.Create(" href=\"", 4862), Tuple.Create("\"", 4897)
            
            #line 85 "..\..\Views\SeasonStatistics.cshtml"
, Tuple.Create(Tuple.Create("", 4869), Tuple.Create<System.Object, System.Int32>(next.SeasonRoute
            
            #line default
            #line hidden
, 4869), false)
, Tuple.Create(Tuple.Create("", 4886), Tuple.Create("/statistics", 4886), true)
);

WriteLiteral(" class=\"btn btn-secondary\"");

WriteLiteral(">Next season</a>\r\n");

            
            #line 86 "..\..\Views\SeasonStatistics.cshtml"
            }

            
            #line default
            #line hidden
WriteLiteral("            <a");

WriteAttribute("href", Tuple.Create(" href=\"", 4971), Tuple.Create("\"", 5032)
            
            #line 87 "..\..\Views\SeasonStatistics.cshtml"
, Tuple.Create(Tuple.Create("", 4978), Tuple.Create<System.Object, System.Int32>(Model.Context.Competition.CompetitionRoute
            
            #line default
            #line hidden
, 4978), false)
, Tuple.Create(Tuple.Create("", 5021), Tuple.Create("/statistics", 5021), true)
);

WriteLiteral(" class=\"btn btn-secondary\"");

WriteLiteral(">All seasons</a>\r\n        </p>\r\n");

            
            #line 89 "..\..\Views\SeasonStatistics.cshtml"
    }

            
            #line default
            #line hidden
WriteLiteral("</div>");

        }
    }
}
#pragma warning restore 1591
