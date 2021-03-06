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
    
    #line 2 "..\..\Views\Partials\_RunOuts.cshtml"
    using Humanizer;
    
    #line default
    #line hidden
    
    #line 3 "..\..\Views\Partials\_RunOuts.cshtml"
    using Stoolball.Dates;
    
    #line default
    #line hidden
    
    #line 4 "..\..\Views\Partials\_RunOuts.cshtml"
    using Stoolball.Matches;
    
    #line default
    #line hidden
    
    #line 5 "..\..\Views\Partials\_RunOuts.cshtml"
    using Stoolball.Web.Statistics;
    
    #line default
    #line hidden
    using Umbraco.Core;
    
    #line 6 "..\..\Views\Partials\_RunOuts.cshtml"
    using Umbraco.Core.Composing;
    
    #line default
    #line hidden
    using Umbraco.Core.Models;
    using Umbraco.Core.Models.PublishedContent;
    using Umbraco.Web;
    using Umbraco.Web.Mvc;
    using Umbraco.Web.PublishedModels;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Views/Partials/_RunOuts.cshtml")]
    public partial class _Views_Partials__RunOuts_cshtml : Umbraco.Web.Mvc.UmbracoViewPage<StatisticsViewModel<PlayerInnings>>
    {
        public _Views_Partials__RunOuts_cshtml()
        {
        }
        public override void Execute()
        {
            
            #line 7 "..\..\Views\Partials\_RunOuts.cshtml"
  
    var dateTimeFormatter = Current.Factory.CreateInstance<DateTimeFormatter>();

            
            #line default
            #line hidden
WriteLiteral("\r\n");

            
            #line 10 "..\..\Views\Partials\_RunOuts.cshtml"
 if (Model.Results.Any())
{

            
            #line default
            #line hidden
WriteLiteral("    <table");

WriteLiteral(" class=\"statistics table-as-cards table-as-cards-reset-sm\"");

WriteLiteral(">\r\n");

            
            #line 13 "..\..\Views\Partials\_RunOuts.cshtml"
        
            
            #line default
            #line hidden
            
            #line 13 "..\..\Views\Partials\_RunOuts.cshtml"
         if (Model.ShowCaption)
        {

            
            #line default
            #line hidden
WriteLiteral("            <caption>Run-outs, most recent first</caption>\r\n");

            
            #line 16 "..\..\Views\Partials\_RunOuts.cshtml"
        }

            
            #line default
            #line hidden
WriteLiteral("        <thead>\r\n            <tr>\r\n                <th");

WriteLiteral(" scope=\"col\"");

WriteLiteral(">Match</th>\r\n                <th");

WriteLiteral(" scope=\"col\"");

WriteLiteral(" class=\"d-none d-sm-table-cell\"");

WriteLiteral(">When</th>\r\n                <th");

WriteLiteral(" scope=\"col\"");

WriteLiteral(">Batter</th>\r\n                <th");

WriteLiteral(" scope=\"col\"");

WriteLiteral(" class=\"numeric\"");

WriteLiteral(">Runs</th>\r\n            </tr>\r\n        </thead>\r\n        <tbody>\r\n");

            
            #line 26 "..\..\Views\Partials\_RunOuts.cshtml"
            
            
            #line default
            #line hidden
            
            #line 26 "..\..\Views\Partials\_RunOuts.cshtml"
              
                var previousMatchRoute = string.Empty;
                for (var i = 0; i < Model.Results.Count(); i++)
                {

            
            #line default
            #line hidden
WriteLiteral("                    <tr>\r\n");

            
            #line 31 "..\..\Views\Partials\_RunOuts.cshtml"
                        
            
            #line default
            #line hidden
            
            #line 31 "..\..\Views\Partials\_RunOuts.cshtml"
                           
                            var rowspan = 1;
                            var row = i+1;
                            while (row < Model.Results.Count())
                            {
                                if (Model.Results[row].Match.MatchRoute == Model.Results[i].Match.MatchRoute)
                                {
                                    rowspan++;
                                    row++;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        
            
            #line default
            #line hidden
WriteLiteral("\r\n\r\n");

            
            #line 48 "..\..\Views\Partials\_RunOuts.cshtml"
                        
            
            #line default
            #line hidden
            
            #line 48 "..\..\Views\Partials\_RunOuts.cshtml"
                         if (Model.Results[i].Match.MatchRoute != previousMatchRoute)
                        {

            
            #line default
            #line hidden
WriteLiteral("                            <td");

WriteAttribute("rowspan", Tuple.Create(" rowspan=\"", 1854), Tuple.Create("\"", 1872)
            
            #line 50 "..\..\Views\Partials\_RunOuts.cshtml"
, Tuple.Create(Tuple.Create("", 1864), Tuple.Create<System.Object, System.Int32>(rowspan
            
            #line default
            #line hidden
, 1864), false)
);

WriteLiteral(" class=\"table-as-cards__header-sm match__stacked\"");

WriteLiteral("><a");

WriteAttribute("href", Tuple.Create(" href=\"", 1925), Tuple.Create("\"", 1966)
            
            #line 50 "..\..\Views\Partials\_RunOuts.cshtml"
                             , Tuple.Create(Tuple.Create("", 1932), Tuple.Create<System.Object, System.Int32>(Model.Results[i].Match.MatchRoute
            
            #line default
            #line hidden
, 1932), false)
);

WriteLiteral(">");

            
            #line 50 "..\..\Views\Partials\_RunOuts.cshtml"
                                                                                                                                            Write(Model.Results[i].Match.MatchName);

            
            #line default
            #line hidden
WriteLiteral("<span");

WriteLiteral(" class=\"d-sm-none\"");

WriteLiteral(">, <span");

WriteLiteral(" class=\"text-nowrap\"");

WriteLiteral(">");

            
            #line 50 "..\..\Views\Partials\_RunOuts.cshtml"
                                                                                                                                                                                                                                  Write(dateTimeFormatter.FormatDate(Model.Results[i].Match.StartTime, false, true, true));

            
            #line default
            #line hidden
WriteLiteral("</span></span></a></td>\r\n");

WriteLiteral("                            <td");

WriteLiteral(" class=\"text-nowrap d-none d-sm-table-cell\"");

WriteAttribute("rowspan", Tuple.Create(" rowspan=\"", 2236), Tuple.Create("\"", 2254)
            
            #line 51 "..\..\Views\Partials\_RunOuts.cshtml"
    , Tuple.Create(Tuple.Create("", 2246), Tuple.Create<System.Object, System.Int32>(rowspan
            
            #line default
            #line hidden
, 2246), false)
);

WriteLiteral("><span");

WriteLiteral(" class=\"table-as-cards__label\"");

WriteLiteral(" aria-hidden=\"true\"");

WriteLiteral(">When</span><span");

WriteLiteral(" class=\"text-nowrap\"");

WriteLiteral(">");

            
            #line 51 "..\..\Views\Partials\_RunOuts.cshtml"
                                                                                                                                                                                      Write(dateTimeFormatter.FormatDate(Model.Results[i].Match.StartTime, false, true, false));

            
            #line default
            #line hidden
WriteLiteral("</span></td>\r\n");

WriteLiteral("                            <td");

WriteLiteral(" data-stackable=\"true\"");

WriteLiteral("><span");

WriteLiteral(" class=\"table-as-cards__label\"");

WriteLiteral(" aria-hidden=\"true\"");

WriteLiteral(">Batter</span><div><a");

WriteAttribute("href", Tuple.Create(" href=\"", 2576), Tuple.Create("\"", 2633)
            
            #line 52 "..\..\Views\Partials\_RunOuts.cshtml"
                                                        , Tuple.Create(Tuple.Create("", 2583), Tuple.Create<System.Object, System.Int32>(Model.Results[i].Result.Batter.Player.PlayerRoute
            
            #line default
            #line hidden
, 2583), false)
);

WriteLiteral(">");

            
            #line 52 "..\..\Views\Partials\_RunOuts.cshtml"
                                                                                                                                                                                        Write(Model.Results[i].Result.Batter.PlayerIdentityName);

            
            #line default
            #line hidden
WriteLiteral("</a></div></td>\r\n");

            
            #line 53 "..\..\Views\Partials\_RunOuts.cshtml"
                        }
                        else
                        {

            
            #line default
            #line hidden
WriteLiteral("                            <td");

WriteLiteral(" data-stackable=\"true\"");

WriteLiteral(" class=\"table-as-cards__rowspan-header-sm\"");

WriteLiteral("><span");

WriteLiteral(" class=\"table-as-cards__label\"");

WriteLiteral(" aria-hidden=\"true\"");

WriteLiteral(">Batter</span><div><a");

WriteAttribute("href", Tuple.Create(" href=\"", 2959), Tuple.Create("\"", 3016)
            
            #line 56 "..\..\Views\Partials\_RunOuts.cshtml"
                                                                                                  , Tuple.Create(Tuple.Create("", 2966), Tuple.Create<System.Object, System.Int32>(Model.Results[i].Result.Batter.Player.PlayerRoute
            
            #line default
            #line hidden
, 2966), false)
);

WriteLiteral(">");

            
            #line 56 "..\..\Views\Partials\_RunOuts.cshtml"
                                                                                                                                                                                                                                  Write(Model.Results[i].Result.Batter.PlayerIdentityName);

            
            #line default
            #line hidden
WriteLiteral("</a></div></td>\r\n");

            
            #line 57 "..\..\Views\Partials\_RunOuts.cshtml"
                        }

            
            #line default
            #line hidden
WriteLiteral("                        ");

            
            #line 58 "..\..\Views\Partials\_RunOuts.cshtml"
                          
                            previousMatchRoute = Model.Results[i].Match.MatchRoute;
                        
            
            #line default
            #line hidden
WriteLiteral("\r\n");

            
            #line 61 "..\..\Views\Partials\_RunOuts.cshtml"
                        
            
            #line default
            #line hidden
            
            #line 61 "..\..\Views\Partials\_RunOuts.cshtml"
                         if (Model.Results[i].Result.RunsScored.HasValue)
                        {

            
            #line default
            #line hidden
WriteLiteral("                            <td");

WriteLiteral(" data-stackable=\"true\"");

WriteLiteral(" class=\"numeric-sm\"");

WriteLiteral("><span");

WriteLiteral(" class=\"table-as-cards__label\"");

WriteLiteral(" aria-hidden=\"true\"");

WriteLiteral(">Runs</span>");

            
            #line 63 "..\..\Views\Partials\_RunOuts.cshtml"
                                                                                                                                      Write(Model.Results[i].Result.RunsScored.Value);

            
            #line default
            #line hidden
WriteLiteral("</td>\r\n");

            
            #line 64 "..\..\Views\Partials\_RunOuts.cshtml"
                        }
                        else
                        {

            
            #line default
            #line hidden
WriteLiteral("                            <td");

WriteLiteral(" data-stackable=\"true\"");

WriteLiteral(" class=\"numeric-sm table-as-cards__missing-data\"");

WriteLiteral(">–</td>\r\n");

            
            #line 68 "..\..\Views\Partials\_RunOuts.cshtml"
                        }

            
            #line default
            #line hidden
WriteLiteral("                    </tr>\r\n");

            
            #line 70 "..\..\Views\Partials\_RunOuts.cshtml"
                }
            
            
            #line default
            #line hidden
WriteLiteral("\r\n        </tbody>\r\n    </table>\r\n");

            
            #line 74 "..\..\Views\Partials\_RunOuts.cshtml"
    
            
            #line default
            #line hidden
            
            #line 74 "..\..\Views\Partials\_RunOuts.cshtml"
Write(Html.Partial("_Paging", Model.StatisticsFilter.Paging));

            
            #line default
            #line hidden
            
            #line 74 "..\..\Views\Partials\_RunOuts.cshtml"
                                                           
}
            
            #line default
            #line hidden
        }
    }
}
#pragma warning restore 1591
