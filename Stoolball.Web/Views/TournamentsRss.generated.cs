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
    
    #line 3 "..\..\Views\TournamentsRss.cshtml"
    using Humanizer;
    
    #line default
    #line hidden
    
    #line 4 "..\..\Views\TournamentsRss.cshtml"
    using Stoolball.Dates;
    
    #line default
    #line hidden
    
    #line 2 "..\..\Views\TournamentsRss.cshtml"
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
    [System.Web.WebPages.PageVirtualPathAttribute("~/Views/TournamentsRss.cshtml")]
    public partial class _Views_TournamentsRss_cshtml : Umbraco.Web.Mvc.UmbracoViewPage<MatchListingViewModel>
    {
        public _Views_TournamentsRss_cshtml()
        {
        }
        public override void Execute()
        {
            
            #line 5 "..\..\Views\TournamentsRss.cshtml"
  
    Layout = null;
    Response.ContentType = "application/rss+xml";
    var canonicalUrl = $"{Request.Url.Scheme}://www.stoolball.org.uk{Request.Url.AbsolutePath.Substring(0, Request.Url.AbsolutePath.Length - 4).Replace("/all", string.Empty)}.rss{Request.Url.Query}";
    Response.Headers.Add("Link", $"<{canonicalUrl}>; rel=\"canonical\"");

            
            #line default
            #line hidden
WriteLiteral("<?xml version=\"1.0\" encoding=\"utf-8\" ?>\r\n<rss");

WriteLiteral(" xmlns:content=\"http://purl.org/rss/1.0/modules/content/\"");

WriteLiteral(" version=\"2.0\"");

WriteLiteral(">\r\n<channel>\r\n    <title>");

            
            #line 13 "..\..\Views\TournamentsRss.cshtml"
      Write(Model.Metadata.PageTitle);

            
            #line default
            #line hidden
WriteLiteral("</title>\r\n    <link>https://www.stoolball.org.uk/tournaments</link>\r\n    <descrip" +
"tion>");

            
            #line 15 "..\..\Views\TournamentsRss.cshtml"
            Write(Model.Metadata.Description);

            
            #line default
            #line hidden
WriteLiteral("</description>\r\n    <pubDate>");

            
            #line 16 "..\..\Views\TournamentsRss.cshtml"
        Write(DateTimeOffset.UtcNow.ToRFC822());

            
            #line default
            #line hidden
WriteLiteral("</pubDate>\r\n    <image>\r\n        <url>https://www.stoolball.org.uk/images/logos/s" +
"toolball-england-rss.gif</url>\r\n        <title>");

            
            #line 19 "..\..\Views\TournamentsRss.cshtml"
          Write(Model.Metadata.PageTitle);

            
            #line default
            #line hidden
WriteLiteral("</title>\r\n        <link>https://www.stoolball.org.uk/tournaments</link>\r\n    </im" +
"age>\r\n    <language>en-GB</language>\r\n    <docs>http://blogs.law.harvard.edu/tec" +
"h/rss</docs>\r\n");

            
            #line 24 "..\..\Views\TournamentsRss.cshtml"
    
            
            #line default
            #line hidden
            
            #line 24 "..\..\Views\TournamentsRss.cshtml"
     foreach (var tournament in Model.Matches)
    {

            
            #line default
            #line hidden
WriteLiteral("    ");

WriteLiteral("<item>\r\n");

WriteLiteral("        <title><![CDATA[");

            
            #line 27 "..\..\Views\TournamentsRss.cshtml"
                   Write(tournament.MatchName);

            
            #line default
            #line hidden
WriteLiteral(", ");

            
            #line 27 "..\..\Views\TournamentsRss.cshtml"
                                          Write(Model.DateTimeFormatter.FormatDate(tournament.StartTime, true, false,false));

            
            #line default
            #line hidden
WriteLiteral("]]></title>\r\n");

WriteLiteral("        <description><![CDATA[");

            
            #line 28 "..\..\Views\TournamentsRss.cshtml"
                         Write(tournament.Description());

            
            #line default
            #line hidden
WriteLiteral("]]></description>\r\n");

WriteLiteral("        <link>");

WriteLiteral("https://www.stoolball.org.uk");

            
            #line 29 "..\..\Views\TournamentsRss.cshtml"
                                        Write(tournament.MatchRoute);

            
            #line default
            #line hidden
WriteLiteral("</link>\r\n");

WriteLiteral("        <pubDate>");

            
            #line 30 "..\..\Views\TournamentsRss.cshtml"
            Write(tournament.LastAuditDate.Value.ToRFC822());

            
            #line default
            #line hidden
WriteLiteral("</pubDate>\r\n");

WriteLiteral("        <guid");

WriteLiteral(" isPermaLink=\"false\"");

WriteLiteral(">");

            
            #line 31 "..\..\Views\TournamentsRss.cshtml"
                             Write(Stoolball.Constants.EntityUriPrefixes.Tournament);

            
            #line default
            #line hidden
            
            #line 31 "..\..\Views\TournamentsRss.cshtml"
                                                                              Write(tournament.MatchId);

            
            #line default
            #line hidden
WriteLiteral("</guid>\r\n");

WriteLiteral("\t\t<source");

WriteAttribute("url", Tuple.Create(" url=\"", 1706), Tuple.Create("\"", 1725)
            
            #line 32 "..\..\Views\TournamentsRss.cshtml"
, Tuple.Create(Tuple.Create("", 1712), Tuple.Create<System.Object, System.Int32>(canonicalUrl
            
            #line default
            #line hidden
, 1712), false)
);

WriteLiteral(" />\r\n");

WriteLiteral("        <category>");

            
            #line 33 "..\..\Views\TournamentsRss.cshtml"
             Write(tournament.PlayerType.Humanize(LetterCasing.LowerCase));

            
            #line default
            #line hidden
WriteLiteral("</category>\r\n");

WriteLiteral("    ");

WriteLiteral("</item>\r\n");

            
            #line 35 "..\..\Views\TournamentsRss.cshtml"
    }

            
            #line default
            #line hidden
WriteLiteral("</channel>\r\n</rss>");

        }
    }
}
#pragma warning restore 1591
