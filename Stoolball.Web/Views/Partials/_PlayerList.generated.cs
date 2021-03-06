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
    
    #line 2 "..\..\Views\Partials\_PlayerList.cshtml"
    using Stoolball.Statistics;
    
    #line default
    #line hidden
    using Umbraco.Core;
    using Umbraco.Core.Models;
    using Umbraco.Core.Models.PublishedContent;
    using Umbraco.Web;
    using Umbraco.Web.Mvc;
    using Umbraco.Web.PublishedModels;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Views/Partials/_PlayerList.cshtml")]
    public partial class _Views_Partials__PlayerList_cshtml : System.Web.Mvc.WebViewPage<List<Player>>
    {
        public _Views_Partials__PlayerList_cshtml()
        {
        }
        public override void Execute()
        {
WriteLiteral("<ul");

WriteLiteral(" class=\"list-columns-wide\"");

WriteLiteral(">\r\n");

            
            #line 4 "..\..\Views\Partials\_PlayerList.cshtml"
    
            
            #line default
            #line hidden
            
            #line 4 "..\..\Views\Partials\_PlayerList.cshtml"
     foreach (var player in Model.OrderBy(x => x.PlayerName()))
    {
        var totalMatches = player.PlayerIdentities.Sum(x => x.TotalMatches);

            
            #line default
            #line hidden
WriteLiteral("        <li><div><a");

WriteAttribute("href", Tuple.Create(" href=\"", 251), Tuple.Create("\"", 277)
            
            #line 7 "..\..\Views\Partials\_PlayerList.cshtml"
, Tuple.Create(Tuple.Create("", 258), Tuple.Create<System.Object, System.Int32>(player.PlayerRoute
            
            #line default
            #line hidden
, 258), false)
);

WriteLiteral(">");

            
            #line 7 "..\..\Views\Partials\_PlayerList.cshtml"
                                          Write(player.PlayerName());

            
            #line default
            #line hidden
WriteLiteral("</a> <span");

WriteLiteral(" class=\"text-nowrap\"");

WriteLiteral(">(");

            
            #line 7 "..\..\Views\Partials\_PlayerList.cshtml"
                                                                                              Write(totalMatches);

            
            #line default
            #line hidden
WriteLiteral(" ");

            
            #line 7 "..\..\Views\Partials\_PlayerList.cshtml"
                                                                                                             Write(totalMatches > 1 ? "matches" : "match");

            
            #line default
            #line hidden
WriteLiteral(")</span></div></li>\r\n");

            
            #line 8 "..\..\Views\Partials\_PlayerList.cshtml"
    }

            
            #line default
            #line hidden
WriteLiteral("</ul>\r\n");

        }
    }
}
#pragma warning restore 1591
