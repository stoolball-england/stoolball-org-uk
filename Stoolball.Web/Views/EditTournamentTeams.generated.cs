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
    
    #line 2 "..\..\Views\EditTournamentTeams.cshtml"
    using ClientDependency.Core.Mvc;
    
    #line default
    #line hidden
    using Examine;
    
    #line 5 "..\..\Views\EditTournamentTeams.cshtml"
    using Stoolball.Security;
    
    #line default
    #line hidden
    
    #line 3 "..\..\Views\EditTournamentTeams.cshtml"
    using Stoolball.Teams;
    
    #line default
    #line hidden
    
    #line 6 "..\..\Views\EditTournamentTeams.cshtml"
    using Stoolball.Web.HtmlHelpers;
    
    #line default
    #line hidden
    
    #line 4 "..\..\Views\EditTournamentTeams.cshtml"
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
    [System.Web.WebPages.PageVirtualPathAttribute("~/Views/EditTournamentTeams.cshtml")]
    public partial class _Views_EditTournamentTeams_cshtml : Umbraco.Web.Mvc.UmbracoViewPage<EditTournamentViewModel>
    {
        public _Views_EditTournamentTeams_cshtml()
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

            
            #line 10 "..\..\Views\EditTournamentTeams.cshtml"
  
    Html.EnableClientValidation();
    Html.EnableUnobtrusiveJavaScript();
    Html.RequiresJs("~/scripts/jquery.validate.min.js");
    Html.RequiresJs("~/scripts/jquery.validate.unobtrusive.min.js");

    Html.RequiresJs("~/js/libs/jquery.autocomplete.min.js", 50);
    Html.RequiresCss("~/css/autocomplete.min.css");

    Html.RequiresCss("~/css/related-items.min.css");
    Html.RequiresJs("~/js/related-items.js");

            
            #line default
            #line hidden
WriteLiteral("\r\n<div");

WriteLiteral(" class=\"container-xl\"");

WriteLiteral(">\r\n    <h1>Teams in the ");

            
            #line 23 "..\..\Views\EditTournamentTeams.cshtml"
                Write(Html.TournamentFullNameAndPlayerType(Model.Tournament, x => Model.DateFormatter.FormatDate(x, false, false, false)));

            
            #line default
            #line hidden
WriteLiteral("</h1>\r\n\r\n");

            
            #line 25 "..\..\Views\EditTournamentTeams.cshtml"
    
            
            #line default
            #line hidden
            
            #line 25 "..\..\Views\EditTournamentTeams.cshtml"
     if (Model.IsAuthorized[AuthorizedAction.EditTournament])
    {
        using (Html.BeginUmbracoForm<EditTournamentTeamsSurfaceController>
            ("UpdateTeams"))
        {
            
            
            #line default
            #line hidden
            
            #line 30 "..\..\Views\EditTournamentTeams.cshtml"
       Write(Html.AntiForgeryToken());

            
            #line default
            #line hidden
            
            #line 30 "..\..\Views\EditTournamentTeams.cshtml"
                                    
            
            
            #line default
            #line hidden
            
            #line 31 "..\..\Views\EditTournamentTeams.cshtml"
       Write(Html.ValidationSummary(true));

            
            #line default
            #line hidden
            
            #line 31 "..\..\Views\EditTournamentTeams.cshtml"
                                         

            
            
            #line default
            #line hidden
            
            #line 33 "..\..\Views\EditTournamentTeams.cshtml"
       Write(Html.HiddenFor(m => Model.UrlReferrer));

            
            #line default
            #line hidden
            
            #line 33 "..\..\Views\EditTournamentTeams.cshtml"
                                                   
            

            
            #line default
            #line hidden
WriteLiteral("            <div");

WriteLiteral(" class=\"form-group\"");

WriteLiteral(">\r\n");

WriteLiteral("                ");

            
            #line 36 "..\..\Views\EditTournamentTeams.cshtml"
           Write(Html.LabelFor(m => Model.Tournament.MaximumTeamsInTournament, RequiredFieldStatus.Optional, new { @class = "has-form-text" }));

            
            #line default
            #line hidden
WriteLiteral("\r\n                <p");

WriteLiteral(" class=\"form-text\"");

WriteLiteral(" id=\"maximum-teams-help\"");

WriteLiteral("><small>Tell us how many teams you have room for and who\'s coming, and we\'ll list" +
" your tournament with how many spaces are left.</small></p>\r\n");

WriteLiteral("                ");

            
            #line 38 "..\..\Views\EditTournamentTeams.cshtml"
           Write(Html.TextBoxFor(m => Model.Tournament.MaximumTeamsInTournament, new { @class = "form-control", aria_describedby = "maximum-teams maximum-teams-help", @type = "number" }));

            
            #line default
            #line hidden
WriteLiteral("\r\n");

WriteLiteral("                ");

            
            #line 39 "..\..\Views\EditTournamentTeams.cshtml"
           Write(Html.ValidationMessageFor(m => Model.Tournament.MaximumTeamsInTournament, null, new { id = "maximum-teams" }));

            
            #line default
            #line hidden
WriteLiteral("\r\n            </div>\r\n");

            
            #line 41 "..\..\Views\EditTournamentTeams.cshtml"


            
            #line default
            #line hidden
WriteLiteral("            <table");

WriteLiteral(" class=\"table table-hover related-items related-items__create\"");

WriteLiteral(" data-related-item=\"team\"");

WriteLiteral(">\r\n                <thead");

WriteLiteral(" class=\"thead-dark\"");

WriteLiteral(">\r\n                    <tr><th");

WriteLiteral(" scope=\"col\"");

WriteLiteral(">Confirmed teams</th><th");

WriteLiteral(" scope=\"col\"");

WriteLiteral("><span");

WriteLiteral(" class=\"related-items__if-not-empty\"");

WriteLiteral(">Team created for this tournament</span></th><th");

WriteLiteral(" scope=\"col\"");

WriteLiteral("></th></tr>\r\n                </thead>\r\n                <tbody>\r\n");

            
            #line 47 "..\..\Views\EditTournamentTeams.cshtml"
                    
            
            #line default
            #line hidden
            
            #line 47 "..\..\Views\EditTournamentTeams.cshtml"
                     for (var i = 0; i < Model.Tournament.Teams.Count; i++)
                    {
                        var displayName = Model.Tournament.Teams[i].Team.UntilYear.HasValue ? Model.Tournament.Teams[i].Team.TeamName + " (no longer active)" : Model.Tournament.Teams[i].Team.TeamName;

            
            #line default
            #line hidden
WriteLiteral("                        <tr");

WriteLiteral(" class=\"related-item__selected\"");

WriteLiteral(">\r\n                            <td");

WriteLiteral(" class=\"related-item__selected__section\"");

WriteLiteral("><div");

WriteLiteral(" class=\"related-item__animate\"");

WriteLiteral(">");

            
            #line 51 "..\..\Views\EditTournamentTeams.cshtml"
                                                                                                      Write(displayName);

            
            #line default
            #line hidden
WriteLiteral("</div></td>\r\n                            <td");

WriteLiteral(" class=\"related-item__selected__section\"");

WriteLiteral("><div");

WriteLiteral(" class=\"related-item__animate\"");

WriteLiteral(">");

            
            #line 52 "..\..\Views\EditTournamentTeams.cshtml"
                                                                                                       Write(Model.Tournament.Teams[i].Team.TeamType == Stoolball.Teams.TeamType.Transient ? "Yes" : "No");

            
            #line default
            #line hidden
WriteLiteral("</div></td>\r\n                            <td");

WriteLiteral(" class=\"related-item__delete related-item__selected__section\"");

WriteLiteral(">\r\n                                <div");

WriteLiteral(" class=\"related-item__animate\"");

WriteLiteral(">\r\n");

WriteLiteral("                                    ");

            
            #line 55 "..\..\Views\EditTournamentTeams.cshtml"
                               Write(Html.Hidden($"Tournament.Teams[{i}].Team.TeamId", Model.Tournament.Teams[i].Team.TeamId, new { @class = "related-item__data related-item__id", data_item = Model.Tournament.Teams[i].Team.TeamId }));

            
            #line default
            #line hidden
WriteLiteral("\r\n");

WriteLiteral("                                    ");

            
            #line 56 "..\..\Views\EditTournamentTeams.cshtml"
                               Write(Html.Hidden($"Tournament.Teams[{i}].Team.TeamName", displayName, new { @class = "related-item__data", data_item = Model.Tournament.Teams[i].Team.TeamId }));

            
            #line default
            #line hidden
WriteLiteral("\r\n                                    <button");

WriteLiteral(" type=\"button\"");

WriteLiteral(" class=\"btn-delete-icon\"");

WriteLiteral(">");

            
            #line 57 "..\..\Views\EditTournamentTeams.cshtml"
                                                                             Write(Html.Partial("_DeleteIcon", $"Remove {Model.Tournament.Teams[i].Team.TeamName} from this tournament"));

            
            #line default
            #line hidden
WriteLiteral("</button>\r\n                                </div>\r\n                            </" +
"td>\r\n                        </tr>\r\n");

            
            #line 61 "..\..\Views\EditTournamentTeams.cshtml"
                    }

            
            #line default
            #line hidden
WriteLiteral("                    <tr>\r\n                        <td");

WriteLiteral(" colspan=\"3\"");

WriteLiteral(">\r\n");

WriteLiteral("                            ");

            
            #line 64 "..\..\Views\EditTournamentTeams.cshtml"
                       Write(Html.Label("team-autocomplete", "Add a team", new { @class = "sr-only" }));

            
            #line default
            #line hidden
WriteLiteral("\r\n");

WriteLiteral("                            ");

            
            #line 65 "..\..\Views\EditTournamentTeams.cshtml"
                       Write(Html.TextBox("team-autocomplete", string.Empty, new
                            {
                                @class = "form-control related-item__search",
                                placeholder = "Add a team",
                                autocomplete = "off",
                                type = "search",
                                data_url = $"/api/teams/autocomplete?teamType={TeamType.LimitedMembership.ToString()}&teamType={TeamType.Occasional.ToString()}&teamType={TeamType.Regular.ToString()}&teamType={TeamType.Representative.ToString()}&teamType={TeamType.SchoolAgeGroup.ToString()}&teamType={TeamType.SchoolClub.ToString()}&teamType={TeamType.SchoolOther.ToString()}",
                                data_template = "team-template",
                                aria_label = "Type a team name and press down arrow to select the team"
                            }));

            
            #line default
            #line hidden
WriteLiteral("\r\n                        </td>\r\n                    </tr>\r\n                </tbo" +
"dy>\r\n            </table>\r\n");

WriteLiteral("            <script");

WriteLiteral(" type=\"text/x-template\"");

WriteLiteral(" id=\"team-template\"");

WriteLiteral(@">
                <table>
                    <tr class=""related-item__selected"">
                        <td class=""related-item__selected__section""><div class=""related-item__animate"">{{value}}</div></td>
                        <td class=""related-item__selected__section""><div class=""related-item__animate"">{{create}}</div></td>
                        <td class=""related-item__delete related-item__selected__section"">
                            <div class=""related-item__animate"">
                                <input name=""Tournament.Teams[0].Team.TeamId"" class=""related-item__data related-item__id"" type=""hidden"" value=""{{data}}"" data-item=""{{data}}"" />
                                <input name=""Tournament.Teams[0].Team.TeamName"" class=""related-item__data"" type=""hidden"" value=""{{value}}"" data-item=""{{data}}"" />
                                <button type=""button"" class=""btn-delete-icon"">");

            
            #line 88 "..\..\Views\EditTournamentTeams.cshtml"
                                                                         Write(Html.Partial("_DeleteIcon", "Remove {{value}} from this tournament"));

            
            #line default
            #line hidden
WriteLiteral("</button>\r\n                            </div>\r\n                        </td>\r\n   " +
"                 </tr>\r\n                </table>\r\n            </script>\r\n");

            
            #line 94 "..\..\Views\EditTournamentTeams.cshtml"


            
            #line default
            #line hidden
WriteLiteral("            <button");

WriteLiteral(" class=\"btn btn-primary\"");

WriteLiteral(">Save teams</button>\r\n");

            
            #line 96 "..\..\Views\EditTournamentTeams.cshtml"
        }
    }
    else
    {
        
            
            #line default
            #line hidden
            
            #line 100 "..\..\Views\EditTournamentTeams.cshtml"
   Write(Html.Partial("_Login"));

            
            #line default
            #line hidden
            
            #line 100 "..\..\Views\EditTournamentTeams.cshtml"
                               
    }

            
            #line default
            #line hidden
WriteLiteral("</div>");

        }
    }
}
#pragma warning restore 1591
