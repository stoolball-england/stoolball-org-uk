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
    
    #line 3 "..\..\Views\DeleteMatchLocation.cshtml"
    using ClientDependency.Core.Mvc;
    
    #line default
    #line hidden
    
    #line 6 "..\..\Views\DeleteMatchLocation.cshtml"
    using Constants = Stoolball.Constants;
    
    #line default
    #line hidden
    using Examine;
    
    #line 2 "..\..\Views\DeleteMatchLocation.cshtml"
    using Humanizer;
    
    #line default
    #line hidden
    
    #line 5 "..\..\Views\DeleteMatchLocation.cshtml"
    using Stoolball.Security;
    
    #line default
    #line hidden
    
    #line 4 "..\..\Views\DeleteMatchLocation.cshtml"
    using Stoolball.Web.MatchLocations;
    
    #line default
    #line hidden
    using Umbraco.Core;
    using Umbraco.Core.Models;
    using Umbraco.Core.Models.PublishedContent;
    using Umbraco.Web;
    using Umbraco.Web.Mvc;
    using Umbraco.Web.PublishedModels;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Views/DeleteMatchLocation.cshtml")]
    public partial class _Views_DeleteMatchLocation_cshtml : Umbraco.Web.Mvc.UmbracoViewPage<Stoolball.Web.MatchLocations.DeleteMatchLocationViewModel>
    {
        public _Views_DeleteMatchLocation_cshtml()
        {
        }
        public override void Execute()
        {
            
            #line 7 "..\..\Views\DeleteMatchLocation.cshtml"
  
    Html.EnableClientValidation();
    Html.EnableUnobtrusiveJavaScript();
    Html.RequiresJs("~/scripts/jquery.validate.min.js");
    Html.RequiresJs("~/scripts/jquery.validate.unobtrusive.min.js");

            
            #line default
            #line hidden
WriteLiteral("\r\n");

DefineSection("head", () => {

WriteLiteral("\r\n    <meta");

WriteLiteral(" name=\"robots\"");

WriteLiteral(" content=\"noindex,follow\"");

WriteLiteral(" />\r\n");

});

WriteLiteral("<div");

WriteLiteral(" class=\"container-xl\"");

WriteLiteral(">\r\n    <h1>Delete ");

            
            #line 17 "..\..\Views\DeleteMatchLocation.cshtml"
          Write(Model.MatchLocation.NameAndLocalityOrTownIfDifferent());

            
            #line default
            #line hidden
WriteLiteral("</h1>\r\n\r\n");

            
            #line 19 "..\..\Views\DeleteMatchLocation.cshtml"
    
            
            #line default
            #line hidden
            
            #line 19 "..\..\Views\DeleteMatchLocation.cshtml"
     if (Model.IsAuthorized[AuthorizedAction.DeleteMatchLocation])
    {
        if (!Model.Deleted)
        {

            
            #line default
            #line hidden
WriteLiteral("            <p>If you delete ");

            
            #line 23 "..\..\Views\DeleteMatchLocation.cshtml"
                        Write(Model.MatchLocation.Name());

            
            #line default
            #line hidden
WriteLiteral(" you will:</p>\r\n");

WriteLiteral("            <ul>\r\n");

            
            #line 25 "..\..\Views\DeleteMatchLocation.cshtml"
                
            
            #line default
            #line hidden
            
            #line 25 "..\..\Views\DeleteMatchLocation.cshtml"
                 if (Model.TotalMatches > 0)
                {

            
            #line default
            #line hidden
WriteLiteral("                    <li>leave ");

            
            #line 27 "..\..\Views\DeleteMatchLocation.cshtml"
                          Write("match".ToQuantity(Model.TotalMatches));

            
            #line default
            #line hidden
WriteLiteral(" with an unknown location</li>\r\n");

WriteLiteral("                    <li>change statistics for the teams and players that have pla" +
"yed here</li>\r\n");

            
            #line 29 "..\..\Views\DeleteMatchLocation.cshtml"
                }
                else
                {

            
            #line default
            #line hidden
WriteLiteral("                    <li>not affect any matches</li>\r\n");

WriteLiteral("                    <li>not affect any statistics</li>\r\n");

            
            #line 34 "..\..\Views\DeleteMatchLocation.cshtml"
                }

            
            #line default
            #line hidden
WriteLiteral("                ");

            
            #line 35 "..\..\Views\DeleteMatchLocation.cshtml"
                 if (Model.MatchLocation.Teams.Count > 0)
                {

            
            #line default
            #line hidden
WriteLiteral("                    <li>leave ");

            
            #line 37 "..\..\Views\DeleteMatchLocation.cshtml"
                         Write(Model.MatchLocation.Teams.Humanize(x => x.TeamName));

            
            #line default
            #line hidden
WriteLiteral(" without their home ground or sports centre</li>\r\n");

            
            #line 38 "..\..\Views\DeleteMatchLocation.cshtml"
                }
                else
                {

            
            #line default
            #line hidden
WriteLiteral("                    <li>not affect any teams</li>\r\n");

            
            #line 42 "..\..\Views\DeleteMatchLocation.cshtml"
                }

            
            #line default
            #line hidden
WriteLiteral("            </ul>\r\n");

WriteLiteral("            <p><strong><strong>You cannot undo this.</strong></strong></p>\r\n");

            
            #line 45 "..\..\Views\DeleteMatchLocation.cshtml"

            using (Html.BeginUmbracoForm<DeleteMatchLocationSurfaceController>
                   ("DeleteMatchLocation"))
            {
                
            
            #line default
            #line hidden
            
            #line 49 "..\..\Views\DeleteMatchLocation.cshtml"
           Write(Html.AntiForgeryToken());

            
            #line default
            #line hidden
            
            #line 49 "..\..\Views\DeleteMatchLocation.cshtml"
                                        

                
            
            #line default
            #line hidden
            
            #line 51 "..\..\Views\DeleteMatchLocation.cshtml"
           Write(Html.HiddenFor(m => Model.ConfirmDeleteRequest.RequiredText));

            
            #line default
            #line hidden
            
            #line 51 "..\..\Views\DeleteMatchLocation.cshtml"
                                                                             

            
            #line default
            #line hidden
WriteLiteral("                <div");

WriteLiteral(" class=\"form-group\"");

WriteLiteral(">\r\n");

WriteLiteral("                    ");

            
            #line 53 "..\..\Views\DeleteMatchLocation.cshtml"
               Write(Html.LabelFor(m => Model.ConfirmDeleteRequest.ConfirmationText, $"If you're sure you wish to continue, type '{Model.MatchLocation.NameAndLocalityOrTownIfDifferent()}' into this box:"));

            
            #line default
            #line hidden
WriteLiteral("\r\n");

WriteLiteral("                    ");

            
            #line 54 "..\..\Views\DeleteMatchLocation.cshtml"
               Write(Html.TextBoxFor(m => Model.ConfirmDeleteRequest.ConfirmationText, new { @class = "form-control", required = "required", aria_describedby = "validation", autocorrect = "off", autocapitalize = "off", autocomplete = "off", spellcheck = "false" }));

            
            #line default
            #line hidden
WriteLiteral("\r\n");

WriteLiteral("                    ");

            
            #line 55 "..\..\Views\DeleteMatchLocation.cshtml"
               Write(Html.ValidationMessageFor(m => Model.ConfirmDeleteRequest.ConfirmationText, null, new { id = "validation" }));

            
            #line default
            #line hidden
WriteLiteral("\r\n                </div>\r\n");

            
            #line 57 "..\..\Views\DeleteMatchLocation.cshtml"


            
            #line default
            #line hidden
WriteLiteral("                <button");

WriteLiteral(" class=\"btn btn-danger btn-delete\"");

WriteLiteral(">Delete ");

            
            #line 58 "..\..\Views\DeleteMatchLocation.cshtml"
                                                            Write(Model.MatchLocation.NameAndLocalityOrTownIfDifferent());

            
            #line default
            #line hidden
WriteLiteral("</button>\r\n");

            
            #line 59 "..\..\Views\DeleteMatchLocation.cshtml"
            }
        }
        else
        {

            
            #line default
            #line hidden
WriteLiteral("            <p>");

            
            #line 63 "..\..\Views\DeleteMatchLocation.cshtml"
          Write(Model.MatchLocation.NameAndLocalityOrTownIfDifferent());

            
            #line default
            #line hidden
WriteLiteral(" has been deleted.</p>\r\n");

WriteLiteral("            <p><a");

WriteLiteral(" class=\"btn btn-primary btn-back\"");

WriteAttribute("href", Tuple.Create(" href=\"", 3004), Tuple.Create("\"", 3045)
            
            #line 64 "..\..\Views\DeleteMatchLocation.cshtml"
, Tuple.Create(Tuple.Create("", 3011), Tuple.Create<System.Object, System.Int32>(Constants.Pages.MatchLocationsUrl
            
            #line default
            #line hidden
, 3011), false)
);

WriteLiteral(">Back to ");

            
            #line 64 "..\..\Views\DeleteMatchLocation.cshtml"
                                                                                                Write(Constants.Pages.MatchLocations);

            
            #line default
            #line hidden
WriteLiteral("</a></p>\r\n");

            
            #line 65 "..\..\Views\DeleteMatchLocation.cshtml"
        }
    }
    else
    {
        
            
            #line default
            #line hidden
            
            #line 69 "..\..\Views\DeleteMatchLocation.cshtml"
   Write(Html.Partial("_Login"));

            
            #line default
            #line hidden
            
            #line 69 "..\..\Views\DeleteMatchLocation.cshtml"
                               
    }

            
            #line default
            #line hidden
WriteLiteral("</div>");

        }
    }
}
#pragma warning restore 1591
