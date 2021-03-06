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
    
    #line 3 "..\..\Views\DeleteTeam.cshtml"
    using ClientDependency.Core.Mvc;
    
    #line default
    #line hidden
    
    #line 6 "..\..\Views\DeleteTeam.cshtml"
    using Constants = Stoolball.Constants;
    
    #line default
    #line hidden
    using Examine;
    
    #line 2 "..\..\Views\DeleteTeam.cshtml"
    using Humanizer;
    
    #line default
    #line hidden
    
    #line 5 "..\..\Views\DeleteTeam.cshtml"
    using Stoolball.Security;
    
    #line default
    #line hidden
    
    #line 4 "..\..\Views\DeleteTeam.cshtml"
    using Stoolball.Web.Teams;
    
    #line default
    #line hidden
    using Umbraco.Core;
    using Umbraco.Core.Models;
    using Umbraco.Core.Models.PublishedContent;
    using Umbraco.Web;
    using Umbraco.Web.Mvc;
    using Umbraco.Web.PublishedModels;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Views/DeleteTeam.cshtml")]
    public partial class _Views_DeleteTeam_cshtml : Umbraco.Web.Mvc.UmbracoViewPage<Stoolball.Web.Teams.DeleteTeamViewModel>
    {
        public _Views_DeleteTeam_cshtml()
        {
        }
        public override void Execute()
        {
            
            #line 7 "..\..\Views\DeleteTeam.cshtml"
  
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

            
            #line 17 "..\..\Views\DeleteTeam.cshtml"
          Write(Model.Team.TeamName);

            
            #line default
            #line hidden
WriteLiteral("</h1>\r\n\r\n");

            
            #line 19 "..\..\Views\DeleteTeam.cshtml"
    
            
            #line default
            #line hidden
            
            #line 19 "..\..\Views\DeleteTeam.cshtml"
     if (Model.IsAuthorized[AuthorizedAction.DeleteTeam])
    {
        if (!Model.Deleted)
        {

            
            #line default
            #line hidden
WriteLiteral("            <p>If you delete ");

            
            #line 23 "..\..\Views\DeleteTeam.cshtml"
                        Write(Model.Team.TeamName);

            
            #line default
            #line hidden
WriteLiteral(" you will:</p>\r\n");

WriteLiteral("            <ul>\r\n");

            
            #line 25 "..\..\Views\DeleteTeam.cshtml"
                
            
            #line default
            #line hidden
            
            #line 25 "..\..\Views\DeleteTeam.cshtml"
                 if (Model.Team.Players.Count > 0)
                {

            
            #line default
            #line hidden
WriteLiteral("                    <li>delete ");

            
            #line 27 "..\..\Views\DeleteTeam.cshtml"
                           Write("player".ToQuantity(Model.Team.Players.Count));

            
            #line default
            #line hidden
WriteLiteral(" and all their statistics and awards</li>\r\n");

            
            #line 28 "..\..\Views\DeleteTeam.cshtml"
                }
                else
                {

            
            #line default
            #line hidden
WriteLiteral("                    <li>not delete any players</li>\r\n");

            
            #line 32 "..\..\Views\DeleteTeam.cshtml"
                }

            
            #line default
            #line hidden
WriteLiteral("                ");

            
            #line 33 "..\..\Views\DeleteTeam.cshtml"
                 if (Model.TotalMatches > 0)
                {

            
            #line default
            #line hidden
WriteLiteral("                    <li>leave ");

            
            #line 35 "..\..\Views\DeleteTeam.cshtml"
                          Write("match".ToQuantity(Model.TotalMatches));

            
            #line default
            #line hidden
WriteLiteral(" with a missing team and an incomplete scorecard</li>\r\n");

WriteLiteral("                    <li>delete statistics for this team</li>\r\n");

WriteLiteral("                    <li>delete statistics for opposition teams and their players<" +
"/li>\r\n");

            
            #line 38 "..\..\Views\DeleteTeam.cshtml"
                }
                else
                {

            
            #line default
            #line hidden
WriteLiteral("                    <li>not affect any matches</li>\r\n");

WriteLiteral("                    <li>not affect any statistics</li>\r\n");

            
            #line 43 "..\..\Views\DeleteTeam.cshtml"
                }

            
            #line default
            #line hidden
WriteLiteral("                ");

            
            #line 44 "..\..\Views\DeleteTeam.cshtml"
                 if (Model.Team.Seasons.Count > 0)
                {

            
            #line default
            #line hidden
WriteLiteral("                    <li>remove this team from ");

            
            #line 46 "..\..\Views\DeleteTeam.cshtml"
                                          Write("competition season".ToQuantity(Model.Team.Seasons.Count));

            
            #line default
            #line hidden
WriteLiteral(", changing the results</li>\r\n");

            
            #line 47 "..\..\Views\DeleteTeam.cshtml"
                }
                else
                {

            
            #line default
            #line hidden
WriteLiteral("                    <li>not affect any competitions</li>\r\n");

            
            #line 51 "..\..\Views\DeleteTeam.cshtml"
                }

            
            #line default
            #line hidden
WriteLiteral("            </ul>\r\n");

WriteLiteral("            <p><strong><strong>You cannot undo this.</strong></strong></p>\r\n");

            
            #line 54 "..\..\Views\DeleteTeam.cshtml"

            using (Html.BeginUmbracoForm<DeleteTeamSurfaceController>
                   ("DeleteTeam"))
            {
                
            
            #line default
            #line hidden
            
            #line 58 "..\..\Views\DeleteTeam.cshtml"
           Write(Html.AntiForgeryToken());

            
            #line default
            #line hidden
            
            #line 58 "..\..\Views\DeleteTeam.cshtml"
                                        

                
            
            #line default
            #line hidden
            
            #line 60 "..\..\Views\DeleteTeam.cshtml"
           Write(Html.HiddenFor(m => Model.ConfirmDeleteRequest.RequiredText));

            
            #line default
            #line hidden
            
            #line 60 "..\..\Views\DeleteTeam.cshtml"
                                                                             

            
            #line default
            #line hidden
WriteLiteral("                <div");

WriteLiteral(" class=\"form-group\"");

WriteLiteral(">\r\n");

WriteLiteral("                    ");

            
            #line 62 "..\..\Views\DeleteTeam.cshtml"
               Write(Html.LabelFor(m => Model.ConfirmDeleteRequest.ConfirmationText, $"If you're sure you wish to continue, type '{Model.Team.TeamName}' into this box:"));

            
            #line default
            #line hidden
WriteLiteral("\r\n");

WriteLiteral("                    ");

            
            #line 63 "..\..\Views\DeleteTeam.cshtml"
               Write(Html.TextBoxFor(m => Model.ConfirmDeleteRequest.ConfirmationText, new { @class = "form-control", required = "required", aria_describedby = "validation", autocorrect = "off", autocapitalize = "off", autocomplete = "off", spellcheck = "false" }));

            
            #line default
            #line hidden
WriteLiteral("\r\n");

WriteLiteral("                    ");

            
            #line 64 "..\..\Views\DeleteTeam.cshtml"
               Write(Html.ValidationMessageFor(m => Model.ConfirmDeleteRequest.ConfirmationText, null, new { id = "validation" }));

            
            #line default
            #line hidden
WriteLiteral("\r\n                </div>\r\n");

            
            #line 66 "..\..\Views\DeleteTeam.cshtml"


            
            #line default
            #line hidden
WriteLiteral("                <button");

WriteLiteral(" class=\"btn btn-danger btn-delete\"");

WriteLiteral(">Delete ");

            
            #line 67 "..\..\Views\DeleteTeam.cshtml"
                                                            Write(Model.Team.TeamName);

            
            #line default
            #line hidden
WriteLiteral("</button>\r\n");

            
            #line 68 "..\..\Views\DeleteTeam.cshtml"
            }
        }
        else
        {

            
            #line default
            #line hidden
WriteLiteral("            <p>");

            
            #line 72 "..\..\Views\DeleteTeam.cshtml"
          Write(Model.Team.TeamName);

            
            #line default
            #line hidden
WriteLiteral(" has been deleted.</p>\r\n");

WriteLiteral("            <p><a");

WriteLiteral(" class=\"btn btn-primary btn-back\"");

WriteAttribute("href", Tuple.Create(" href=\"", 3207), Tuple.Create("\"", 3239)
            
            #line 73 "..\..\Views\DeleteTeam.cshtml"
, Tuple.Create(Tuple.Create("", 3214), Tuple.Create<System.Object, System.Int32>(Constants.Pages.TeamsUrl
            
            #line default
            #line hidden
, 3214), false)
);

WriteLiteral(">Back to ");

            
            #line 73 "..\..\Views\DeleteTeam.cshtml"
                                                                                       Write(Constants.Pages.Teams);

            
            #line default
            #line hidden
WriteLiteral("</a></p>\r\n");

            
            #line 74 "..\..\Views\DeleteTeam.cshtml"
        }
    }
    else
    {
        
            
            #line default
            #line hidden
            
            #line 78 "..\..\Views\DeleteTeam.cshtml"
   Write(Html.Partial("_Login"));

            
            #line default
            #line hidden
            
            #line 78 "..\..\Views\DeleteTeam.cshtml"
                               
    }

            
            #line default
            #line hidden
WriteLiteral("</div>");

        }
    }
}
#pragma warning restore 1591
