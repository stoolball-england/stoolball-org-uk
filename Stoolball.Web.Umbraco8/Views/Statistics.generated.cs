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
    
    #line 7 "..\..\Views\Statistics.cshtml"
    using ClientDependency.Core.Mvc;
    
    #line default
    #line hidden
    using Examine;
    
    #line 3 "..\..\Views\Statistics.cshtml"
    using Stoolball.Matches;
    
    #line default
    #line hidden
    
    #line 2 "..\..\Views\Statistics.cshtml"
    using Stoolball.Security;
    
    #line default
    #line hidden
    
    #line 4 "..\..\Views\Statistics.cshtml"
    using Stoolball.Statistics;
    
    #line default
    #line hidden
    
    #line 6 "..\..\Views\Statistics.cshtml"
    using Stoolball.Web.Filtering;
    
    #line default
    #line hidden
    
    #line 5 "..\..\Views\Statistics.cshtml"
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
    [System.Web.WebPages.PageVirtualPathAttribute("~/Views/Statistics.cshtml")]
    public partial class _Views_Statistics_cshtml : Umbraco.Web.Mvc.UmbracoViewPage<StatisticsSummaryViewModel>
    {
        public _Views_Statistics_cshtml()
        {
        }
        public override void Execute()
        {
            
            #line 8 "..\..\Views\Statistics.cshtml"
  
    Html.RequiresCss("/statistics/statistics.min.css");
    Html.RequiresJs("/js/filter.js");
    Html.RequiresCss("/css/filter.min.css");

    var individualScores = new StatisticsViewModel<PlayerInnings>(Model, Services.UserService) { ShowCaption = true, ShowPlayerColumn = true, DefaultFilter = Model.DefaultFilter, AppliedFilter = Model.AppliedFilter };
    individualScores.Results.AddRange(Model.PlayerInnings);
    var bowlingFigures = new StatisticsViewModel<BowlingFigures>(Model, Services.UserService) { ShowCaption = true, ShowPlayerColumn = true, DefaultFilter = Model.DefaultFilter, AppliedFilter = Model.AppliedFilter };
    bowlingFigures.Results.AddRange(Model.BowlingFigures);
    var mostRuns = new StatisticsViewModel<BestStatistic>(Model, Services.UserService) { ShowCaption = true, DefaultFilter = Model.DefaultFilter, AppliedFilter = Model.AppliedFilter };
    mostRuns.Results.AddRange(Model.MostRuns);
    var mostWickets = new StatisticsViewModel<BestStatistic>(Model, Services.UserService) { ShowCaption = true, DefaultFilter = Model.DefaultFilter, AppliedFilter = Model.AppliedFilter };
    mostWickets.Results.AddRange(Model.MostWickets);
    var mostCatches = new StatisticsViewModel<BestStatistic>(Model, Services.UserService) { ShowCaption = true, DefaultFilter = Model.DefaultFilter, AppliedFilter = Model.AppliedFilter };
    mostCatches.Results.AddRange(Model.MostCatches);

            
            #line default
            #line hidden
WriteLiteral("\r\n");

DefineSection("head", () => {

WriteLiteral("\r\n    <meta");

WriteLiteral(" name=\"robots\"");

WriteLiteral(" content=\"noindex, follow\"");

WriteLiteral(" />\r\n");

});

WriteLiteral("<div");

WriteLiteral(" class=\"container-xl\"");

WriteLiteral(">\r\n    <h1>Statistics for all teams</h1>\r\n\r\n    <ul");

WriteLiteral(" class=\"nav nav-tabs\"");

WriteLiteral(">\r\n        <li");

WriteLiteral(" class=\"nav-item nav-item-admin\"");

WriteLiteral(">\r\n            <button");

WriteLiteral(" type=\"button\"");

WriteLiteral(" class=\"nav-link nav-link-filter\"");

WriteLiteral(">Edit filter</button>\r\n        </li>\r\n");

            
            #line 34 "..\..\Views\Statistics.cshtml"
        
            
            #line default
            #line hidden
            
            #line 34 "..\..\Views\Statistics.cshtml"
         if (Model.IsAuthorized[AuthorizedAction.EditStatistics])
        {

            
            #line default
            #line hidden
WriteLiteral("            <li");

WriteLiteral(" class=\"nav-item nav-item-admin\"");

WriteLiteral(">\r\n                <a");

WriteLiteral(" class=\"nav-link nav-link-edit\"");

WriteAttribute("href", Tuple.Create(" href=\"", 2182), Tuple.Create("\"", 2234)
            
            #line 37 "..\..\Views\Statistics.cshtml"
, Tuple.Create(Tuple.Create("", 2189), Tuple.Create<System.Object, System.Int32>(Stoolball.Constants.Pages.StatisticsUrl
            
            #line default
            #line hidden
, 2189), false)
, Tuple.Create(Tuple.Create("", 2229), Tuple.Create("/edit", 2229), true)
);

WriteLiteral(">Edit statistics</a>\r\n            </li>\r\n");

            
            #line 39 "..\..\Views\Statistics.cshtml"
        }

            
            #line default
            #line hidden
WriteLiteral("    </ul>\r\n");

WriteLiteral("    ");

            
            #line 41 "..\..\Views\Statistics.cshtml"
Write(Html.Partial("_Filters", new FilterViewModel
{
    FilteredItemTypeSingular = "Statistics",
    FilteredItemTypePlural = "Statistics",
    FilterDescription = Model.FilterDescription,
    from = Model.AppliedFilter.FromDate,
    to = Model.AppliedFilter.UntilDate
}));

            
            #line default
            #line hidden
WriteLiteral("\r\n\r\n");

            
            #line 50 "..\..\Views\Statistics.cshtml"
    
            
            #line default
            #line hidden
            
            #line 50 "..\..\Views\Statistics.cshtml"
     if (!Model.PlayerInnings.Any() && !Model.BowlingFigures.Any() && !Model.MostRuns.Any() && !Model.MostWickets.Any() && !Model.MostCatches.Any())
    {
        
            
            #line default
            #line hidden
            
            #line 52 "..\..\Views\Statistics.cshtml"
   Write(Html.Partial("_NoData"));

            
            #line default
            #line hidden
            
            #line 52 "..\..\Views\Statistics.cshtml"
                                
    }
    else
    {
        
            
            #line default
            #line hidden
            
            #line 56 "..\..\Views\Statistics.cshtml"
   Write(Html.Partial("_StatisticsBasis"));

            
            #line default
            #line hidden
            
            #line 56 "..\..\Views\Statistics.cshtml"
                                         
        
            
            #line default
            #line hidden
            
            #line 57 "..\..\Views\Statistics.cshtml"
   Write(Html.Partial("_IndividualScores", individualScores));

            
            #line default
            #line hidden
            
            #line 57 "..\..\Views\Statistics.cshtml"
                                                            
        
            
            #line default
            #line hidden
            
            #line 58 "..\..\Views\Statistics.cshtml"
   Write(Html.Partial("_MostRuns", mostRuns));

            
            #line default
            #line hidden
            
            #line 58 "..\..\Views\Statistics.cshtml"
                                            
        
            
            #line default
            #line hidden
            
            #line 59 "..\..\Views\Statistics.cshtml"
   Write(Html.Partial("_BattingAverage", new StatisticsViewModel<BestStatistic>(Model, Services.UserService) { ShowLinkOnly = true, DefaultFilter = Model.DefaultFilter, AppliedFilter = Model.AppliedFilter }));

            
            #line default
            #line hidden
            
            #line 59 "..\..\Views\Statistics.cshtml"
                                                                                                                                                                                                               
        
            
            #line default
            #line hidden
            
            #line 60 "..\..\Views\Statistics.cshtml"
   Write(Html.Partial("_BattingStrikeRate", new StatisticsViewModel<BestStatistic>(Model, Services.UserService) { ShowLinkOnly = true, DefaultFilter = Model.DefaultFilter, AppliedFilter = Model.AppliedFilter }));

            
            #line default
            #line hidden
            
            #line 60 "..\..\Views\Statistics.cshtml"
                                                                                                                                                                                                                  
        
            
            #line default
            #line hidden
            
            #line 61 "..\..\Views\Statistics.cshtml"
   Write(Html.Partial("_BowlingFigures", bowlingFigures));

            
            #line default
            #line hidden
            
            #line 61 "..\..\Views\Statistics.cshtml"
                                                        
        
            
            #line default
            #line hidden
            
            #line 62 "..\..\Views\Statistics.cshtml"
   Write(Html.Partial("_MostWickets", mostWickets));

            
            #line default
            #line hidden
            
            #line 62 "..\..\Views\Statistics.cshtml"
                                                  
        
            
            #line default
            #line hidden
            
            #line 63 "..\..\Views\Statistics.cshtml"
   Write(Html.Partial("_BowlingAverage", new StatisticsViewModel<BestStatistic>(Model, Services.UserService) { ShowLinkOnly = true, DefaultFilter = Model.DefaultFilter, AppliedFilter = Model.AppliedFilter }));

            
            #line default
            #line hidden
            
            #line 63 "..\..\Views\Statistics.cshtml"
                                                                                                                                                                                                               
        
            
            #line default
            #line hidden
            
            #line 64 "..\..\Views\Statistics.cshtml"
   Write(Html.Partial("_EconomyRate", new StatisticsViewModel<BestStatistic>(Model, Services.UserService) { ShowLinkOnly = true, DefaultFilter = Model.DefaultFilter, AppliedFilter = Model.AppliedFilter }));

            
            #line default
            #line hidden
            
            #line 64 "..\..\Views\Statistics.cshtml"
                                                                                                                                                                                                            
        
            
            #line default
            #line hidden
            
            #line 65 "..\..\Views\Statistics.cshtml"
   Write(Html.Partial("_BowlingStrikeRate", new StatisticsViewModel<BestStatistic>(Model, Services.UserService) { ShowLinkOnly = true, DefaultFilter = Model.DefaultFilter, AppliedFilter = Model.AppliedFilter }));

            
            #line default
            #line hidden
            
            #line 65 "..\..\Views\Statistics.cshtml"
                                                                                                                                                                                                                  
        
            
            #line default
            #line hidden
            
            #line 66 "..\..\Views\Statistics.cshtml"
   Write(Html.Partial("_MostCatches", mostCatches));

            
            #line default
            #line hidden
            
            #line 66 "..\..\Views\Statistics.cshtml"
                                                  
        
            
            #line default
            #line hidden
            
            #line 67 "..\..\Views\Statistics.cshtml"
   Write(Html.Partial("_MostRunOuts", new StatisticsViewModel<BestStatistic>(Model, Services.UserService) { ShowLinkOnly = true, DefaultFilter = Model.DefaultFilter, AppliedFilter = Model.AppliedFilter }));

            
            #line default
            #line hidden
            
            #line 67 "..\..\Views\Statistics.cshtml"
                                                                                                                                                                                                            
    }

            
            #line default
            #line hidden
WriteLiteral("</div>");

        }
    }
}
#pragma warning restore 1591