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
    
    #line 3 "..\..\Views\Partials\_Comments.cshtml"
    using Stoolball.Account;
    
    #line default
    #line hidden
    
    #line 2 "..\..\Views\Partials\_Comments.cshtml"
    using Stoolball.Comments;
    
    #line default
    #line hidden
    
    #line 5 "..\..\Views\Partials\_Comments.cshtml"
    using Stoolball.Dates;
    
    #line default
    #line hidden
    using Umbraco.Core;
    
    #line 4 "..\..\Views\Partials\_Comments.cshtml"
    using Umbraco.Core.Composing;
    
    #line default
    #line hidden
    using Umbraco.Core.Models;
    using Umbraco.Core.Models.PublishedContent;
    using Umbraco.Web;
    using Umbraco.Web.Mvc;
    using Umbraco.Web.PublishedModels;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Views/Partials/_Comments.cshtml")]
    public partial class _Views_Partials__Comments_cshtml : System.Web.Mvc.WebViewPage<List<HtmlComment>>
    {
        public _Views_Partials__Comments_cshtml()
        {
        }
        public override void Execute()
        {
            
            #line 6 "..\..\Views\Partials\_Comments.cshtml"
   
    var dateFormatter = (IDateTimeFormatter)Current.Factory.GetInstance(typeof(IDateTimeFormatter));

            
            #line default
            #line hidden
WriteLiteral("\r\n");

            
            #line 9 "..\..\Views\Partials\_Comments.cshtml"
 if (Model.Any())
{

            
            #line default
            #line hidden
WriteLiteral("    <h2");

WriteLiteral(" class=\"comments\"");

WriteLiteral(">Comments</h2>\r\n");

WriteLiteral("    <ol");

WriteLiteral(" class=\"list-unstyled\"");

WriteLiteral(">\r\n");

            
            #line 13 "..\..\Views\Partials\_Comments.cshtml"
        
            
            #line default
            #line hidden
            
            #line 13 "..\..\Views\Partials\_Comments.cshtml"
         foreach (var comment in Model)
        {

            
            #line default
            #line hidden
WriteLiteral("        <li");

WriteLiteral(" class=\"comment\"");

WriteLiteral(">\r\n            <h3");

WriteLiteral(" class=\"comment__credit\"");

WriteLiteral(">\r\n");

            
            #line 17 "..\..\Views\Partials\_Comments.cshtml"
                    
            
            #line default
            #line hidden
            
            #line 17 "..\..\Views\Partials\_Comments.cshtml"
                     if (!string.IsNullOrEmpty(comment.MemberEmail))
                    {
                        var gravatar = new Gravatar(comment.MemberEmail);

            
            #line default
            #line hidden
WriteLiteral("                        <img");

WriteAttribute("src", Tuple.Create(" src=\"", 658), Tuple.Create("\"", 677)
            
            #line 20 "..\..\Views\Partials\_Comments.cshtml"
, Tuple.Create(Tuple.Create("", 664), Tuple.Create<System.Object, System.Int32>(gravatar.Url
            
            #line default
            #line hidden
, 664), false)
);

WriteLiteral(" alt=\"\"");

WriteAttribute("width", Tuple.Create(" width=\"", 685), Tuple.Create("\"", 707)
            
            #line 20 "..\..\Views\Partials\_Comments.cshtml"
, Tuple.Create(Tuple.Create("", 693), Tuple.Create<System.Object, System.Int32>(gravatar.Size
            
            #line default
            #line hidden
, 693), false)
);

WriteAttribute("height", Tuple.Create(" height=\"", 708), Tuple.Create("\"", 731)
            
            #line 20 "..\..\Views\Partials\_Comments.cshtml"
        , Tuple.Create(Tuple.Create("", 717), Tuple.Create<System.Object, System.Int32>(gravatar.Size
            
            #line default
            #line hidden
, 717), false)
);

WriteLiteral(" class=\"comment__account-image\"");

WriteLiteral(" />\r\n");

            
            #line 21 "..\..\Views\Partials\_Comments.cshtml"
                    }

            
            #line default
            #line hidden
WriteLiteral("                    ");

            
            #line 22 "..\..\Views\Partials\_Comments.cshtml"
               Write(comment.MemberName);

            
            #line default
            #line hidden
WriteLiteral(" at ");

            
            #line 22 "..\..\Views\Partials\_Comments.cshtml"
                                      Write(dateFormatter.FormatDateTime(comment.CommentDate, true, true, true));

            
            #line default
            #line hidden
WriteLiteral("\r\n            </h3>\r\n");

WriteLiteral("            ");

            
            #line 24 "..\..\Views\Partials\_Comments.cshtml"
       Write(Html.Raw(comment.Comment));

            
            #line default
            #line hidden
WriteLiteral("\r\n        </li>\r\n");

            
            #line 26 "..\..\Views\Partials\_Comments.cshtml"

        }

            
            #line default
            #line hidden
WriteLiteral("    </ol>\r\n");

WriteLiteral("    <p");

WriteLiteral(" class=\"alert alert-info\"");

WriteLiteral(">Comments are now closed.</p>\r\n");

            
            #line 30 "..\..\Views\Partials\_Comments.cshtml"
}
            
            #line default
            #line hidden
        }
    }
}
#pragma warning restore 1591