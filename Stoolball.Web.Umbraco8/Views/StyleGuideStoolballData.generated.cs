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
    
    #line 3 "..\..\Views\StyleGuideStoolballData.cshtml"
    using ClientDependency.Core.Mvc;
    
    #line default
    #line hidden
    
    #line 2 "..\..\Views\StyleGuideStoolballData.cshtml"
    using ContentModels = Umbraco.Web.PublishedModels;
    
    #line default
    #line hidden
    using Examine;
    
    #line 4 "..\..\Views\StyleGuideStoolballData.cshtml"
    using Stoolball.Comments;
    
    #line default
    #line hidden
    
    #line 6 "..\..\Views\StyleGuideStoolballData.cshtml"
    using Stoolball.Competitions;
    
    #line default
    #line hidden
    
    #line 7 "..\..\Views\StyleGuideStoolballData.cshtml"
    using Stoolball.Matches;
    
    #line default
    #line hidden
    
    #line 10 "..\..\Views\StyleGuideStoolballData.cshtml"
    using Stoolball.Statistics;
    
    #line default
    #line hidden
    
    #line 5 "..\..\Views\StyleGuideStoolballData.cshtml"
    using Stoolball.Teams;
    
    #line default
    #line hidden
    
    #line 9 "..\..\Views\StyleGuideStoolballData.cshtml"
    using Stoolball.Web.Competitions;
    
    #line default
    #line hidden
    
    #line 8 "..\..\Views\StyleGuideStoolballData.cshtml"
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
    [System.Web.WebPages.PageVirtualPathAttribute("~/Views/StyleGuideStoolballData.cshtml")]
    public partial class _Views_StyleGuideStoolballData_cshtml : Umbraco.Web.Mvc.UmbracoViewPage<ContentModels.StyleGuide>
    {
        public _Views_StyleGuideStoolballData_cshtml()
        {
        }
        public override void Execute()
        {
            
            #line 11 "..\..\Views\StyleGuideStoolballData.cshtml"
  
    Html.RequiresJs("https://maps.google.co.uk/maps/api/js?key=" + Model.GoogleMapsApiKey, 50);
    Html.RequiresJs("/js/maps.js", 90);
    Html.RequiresJs("/matchlocations/match-location.js");

    Html.RequiresJs("~/js/libs/jquery.autocomplete.min.js", 50);
    Html.RequiresCss("~/css/autocomplete.min.css");

    Html.RequiresJs("/umbraco/lib/tinymce/tinymce.min.js", 90);
    Html.RequiresJs("/js/tinymce.js");

    Html.RequiresCss("~/matches/scorecards.min.css");
    Html.RequiresCss("~/css/steps.min.css");

    Html.RequiresJs("~/matches/player-autocomplete.js", 70);
    Html.RequiresJs("~/matches/edit-batting-scorecard.js");
    Html.RequiresJs("~/matches/edit-bowling-scorecard.js");

    Html.RequiresCss("~/css/comments.min.css");

            
            #line default
            #line hidden
WriteLiteral("\r\n<div");

WriteLiteral(" class=\"container-xl\"");

WriteLiteral(">\r\n    <h1");

WriteLiteral(" data-show-consent=\"true\"");

WriteLiteral(">");

            
            #line 32 "..\..\Views\StyleGuideStoolballData.cshtml"
                            Write(Model.Name);

            
            #line default
            #line hidden
WriteLiteral("</h1>\r\n    <ul");

WriteLiteral(" class=\"nav nav-tabs nav-tabs-has-add nav-tabs-has-edit\"");

WriteLiteral(">\r\n        <li");

WriteLiteral(" class=\"nav-item\"");

WriteLiteral(">\r\n            <a");

WriteLiteral(" class=\"nav-link\"");

WriteAttribute("href", Tuple.Create(" href=\"", 1338), Tuple.Create("\"", 1379)
            
            #line 35 "..\..\Views\StyleGuideStoolballData.cshtml"
, Tuple.Create(Tuple.Create("", 1345), Tuple.Create<System.Object, System.Int32>(Umbraco.AssignedContentItem.Url()
            
            #line default
            #line hidden
, 1345), false)
);

WriteLiteral(">Umbraco content</a>\r\n        </li>\r\n        <li");

WriteLiteral(" class=\"nav-item\"");

WriteLiteral(">\r\n            <a");

WriteLiteral(" class=\"nav-link\"");

WriteAttribute("href", Tuple.Create(" href=\"", 1479), Tuple.Create("\"", 1548)
            
            #line 38 "..\..\Views\StyleGuideStoolballData.cshtml"
, Tuple.Create(Tuple.Create("", 1486), Tuple.Create<System.Object, System.Int32>(Umbraco.AssignedContentItem.Url()
            
            #line default
            #line hidden
, 1486), false)
, Tuple.Create(Tuple.Create("", 1520), Tuple.Create("?alttemplate=styleguideforms", 1520), true)
);

WriteLiteral(">Forms</a>\r\n        </li>\r\n        <li");

WriteLiteral(" class=\"nav-item\"");

WriteLiteral(">\r\n            <em");

WriteLiteral(" class=\"nav-link active\"");

WriteLiteral(">Stoolball data</em>\r\n        </li>\r\n    </ul>\r\n    <table");

WriteLiteral(" class=\"table\"");

WriteLiteral(">\r\n        <caption>Table with edit options</caption>\r\n        <thead><tr><th>Exa" +
"mple</th><th>Date</th><th>Number</th></tr></thead>\r\n        <tbody>\r\n           " +
" <tr>\r\n                <th");

WriteLiteral(" scope=\"row\"");

WriteLiteral(">Unvalidated</th>\r\n                <td><input");

WriteLiteral(" class=\"related-item__data\"");

WriteLiteral(" data-item=\"3040d68b-bc9e-4bad-a8fe-354b72f214c1\"");

WriteLiteral(" id=\"Season_Teams_0__WithdrawnDate\"");

WriteLiteral(" name=\"Season.Teams[0].WithdrawnDate\"");

WriteLiteral(" type=\"date\"");

WriteLiteral(" value=\"\"");

WriteLiteral("></td>\r\n                <td>\r\n                    <input");

WriteLiteral(" data-val=\"true\"");

WriteLiteral(" data-val-number=\"The field Points for away team must be a number.\"");

WriteLiteral(" data-val-required=\"The Points for away team field is required.\"");

WriteLiteral(" id=\"Season_PointsRules_0__AwayPoints\"");

WriteLiteral(" maxlength=\"2\"");

WriteLiteral(" name=\"Season.PointsRules[0].AwayPoints\"");

WriteLiteral(" type=\"number\"");

WriteLiteral(" value=\"2\"");

WriteLiteral(">\r\n                </td>\r\n            </tr>\r\n            <tr>\r\n                <t" +
"h");

WriteLiteral(" scope=\"row\"");

WriteLiteral(">Valid</th>\r\n                <td>\r\n                    <input");

WriteLiteral(" class=\"form-control valid\"");

WriteLiteral(" data-val=\"true\"");

WriteLiteral(" data-val-required=\"The Match date field is required.\"");

WriteLiteral(" id=\"valid-date-field-in-table\"");

WriteLiteral(" name=\"valid-date-field-in-table\"");

WriteLiteral(" required=\"required\"");

WriteLiteral(" type=\"date\"");

WriteLiteral(" value=\"\"");

WriteLiteral(" aria-invalid=\"false\"");

WriteLiteral(">\r\n                </td>\r\n                <td>\r\n                    <input");

WriteLiteral(" data-val=\"true\"");

WriteLiteral(" data-val-number=\"The field Points for home team must be a number.\"");

WriteLiteral(" data-val-required=\"The Points for home team field is required.\"");

WriteLiteral(" id=\"Season_PointsRules_0__HomePoints\"");

WriteLiteral(" maxlength=\"2\"");

WriteLiteral(" name=\"Season.PointsRules[0].HomePoints\"");

WriteLiteral(" type=\"number\"");

WriteLiteral(" value=\"0\"");

WriteLiteral(" aria-describedby=\"Season_PointsRules_0__HomePoints-error\"");

WriteLiteral(" class=\"valid\"");

WriteLiteral(" aria-invalid=\"false\"");

WriteLiteral(">\r\n                </td>\r\n            </tr>\r\n            <tr>\r\n                <t" +
"h");

WriteLiteral(" scope=\"row\"");

WriteLiteral(">Invalid</th>\r\n                <td>Not applicable</td>\r\n                <td>\r\n   " +
"                 <input");

WriteLiteral(" data-val=\"true\"");

WriteLiteral(" data-val-number=\"The field Points for away team must be a number.\"");

WriteLiteral(" data-val-required=\"The Points for away team field is required.\"");

WriteLiteral(" id=\"Season_PointsRules_0__AwayPoints\"");

WriteLiteral(" maxlength=\"2\"");

WriteLiteral(" name=\"Season.PointsRules[0].AwayPoints\"");

WriteLiteral(" type=\"number\"");

WriteLiteral(" value=\"2\"");

WriteLiteral(" class=\"input-validation-error\"");

WriteLiteral(" aria-describedby=\"Season_PointsRules_0__AwayPoints-error\"");

WriteLiteral(" aria-invalid=\"true\"");

WriteLiteral(">\r\n                    <span");

WriteLiteral(" class=\"field-validation-error\"");

WriteLiteral(" data-valmsg-for=\"Season.PointsRules[0].AwayPoints\"");

WriteLiteral(" data-valmsg-replace=\"true\"");

WriteLiteral("><span");

WriteLiteral(" id=\"Season_PointsRules_0__AwayPoints-error\"");

WriteLiteral(" class=\"\"");

WriteLiteral(">The Points for away team field is required.</span></span>\r\n                </td>" +
"\r\n            </tr>\r\n        </tbody>\r\n    </table>\r\n    <p>A paragraph followin" +
"g a Bootstrap table.</p>\r\n    <table");

WriteLiteral(" class=\"table table-as-cards table-as-cards-reset-md\"");

WriteLiteral(">\r\n        <caption>League table caption. Table displays as cards on mobile.</cap" +
"tion>\r\n        <thead>\r\n            <tr>\r\n                <th");

WriteLiteral(" scope=\"col\"");

WriteLiteral(">Team</th>\r\n                <th");

WriteLiteral(" scope=\"col\"");

WriteLiteral(">Played</th>\r\n                <th");

WriteLiteral(" scope=\"col\"");

WriteLiteral(">Won</th>\r\n                <th");

WriteLiteral(" scope=\"col\"");

WriteLiteral(">Lost</th>\r\n                <th");

WriteLiteral(" scope=\"col\"");

WriteLiteral(">Tied</th>\r\n                <th");

WriteLiteral(" scope=\"col\"");

WriteLiteral(">No result</th>\r\n                <th>Runs scored</th>\r\n                <th>Runs c" +
"onceded</th>\r\n                <th>Points</th>\r\n            </tr>\r\n        </thea" +
"d>\r\n        <tbody>\r\n            <tr>\r\n                <th");

WriteLiteral(" scope=\"row\"");

WriteLiteral("><a");

WriteLiteral(" href=\"https://example.org\"");

WriteLiteral(">Team A</a></th>\r\n                <td");

WriteLiteral(" data-stackable=\"true\"");

WriteLiteral("><span");

WriteLiteral(" class=\"table-as-cards__label\"");

WriteLiteral(" aria-hidden=\"true\"");

WriteLiteral(">Played</span>12</td>\r\n                <td");

WriteLiteral(" data-stackable=\"true\"");

WriteLiteral("><span");

WriteLiteral(" class=\"table-as-cards__label\"");

WriteLiteral(" aria-hidden=\"true\"");

WriteLiteral(">Won</span>9</td>\r\n                <td");

WriteLiteral(" data-stackable=\"true\"");

WriteLiteral("><span");

WriteLiteral(" class=\"table-as-cards__label\"");

WriteLiteral(" aria-hidden=\"true\"");

WriteLiteral(">Lost</span>3</td>\r\n                <td");

WriteLiteral(" data-stackable=\"true\"");

WriteLiteral("><span");

WriteLiteral(" class=\"table-as-cards__label\"");

WriteLiteral(" aria-hidden=\"true\"");

WriteLiteral(">Tied</span>0</td>\r\n                <td");

WriteLiteral(" data-stackable=\"true\"");

WriteLiteral("><span");

WriteLiteral(" class=\"table-as-cards__label\"");

WriteLiteral(" aria-hidden=\"true\"");

WriteLiteral(">No result</span>0</td>\r\n                <td");

WriteLiteral(" data-stackable=\"true\"");

WriteLiteral("><span");

WriteLiteral(" class=\"table-as-cards__label\"");

WriteLiteral(" aria-hidden=\"true\"");

WriteLiteral(">Runs scored</span>0</td>\r\n                <td");

WriteLiteral(" data-stackable=\"true\"");

WriteLiteral("><span");

WriteLiteral(" class=\"table-as-cards__label\"");

WriteLiteral(" aria-hidden=\"true\"");

WriteLiteral(">Runs conceded</span>0</td>\r\n                <td");

WriteLiteral(" data-stackable=\"true\"");

WriteLiteral("><span");

WriteLiteral(" class=\"table-as-cards__label\"");

WriteLiteral(" aria-hidden=\"true\"");

WriteLiteral(">Points</span>18</td>\r\n            </tr>\r\n            <tr>\r\n                <th");

WriteLiteral(" scope=\"row\"");

WriteLiteral("><a");

WriteLiteral(" href=\"https://example.org\"");

WriteLiteral(">Team B</a></th>\r\n                <td");

WriteLiteral(" data-stackable=\"true\"");

WriteLiteral("><span");

WriteLiteral(" class=\"table-as-cards__label\"");

WriteLiteral(" aria-hidden=\"true\"");

WriteLiteral(">Played</span>12</td>\r\n                <td");

WriteLiteral(" data-stackable=\"true\"");

WriteLiteral("><span");

WriteLiteral(" class=\"table-as-cards__label\"");

WriteLiteral(" aria-hidden=\"true\"");

WriteLiteral(">Won</span>8</td>\r\n                <td");

WriteLiteral(" data-stackable=\"true\"");

WriteLiteral("><span");

WriteLiteral(" class=\"table-as-cards__label\"");

WriteLiteral(" aria-hidden=\"true\"");

WriteLiteral(">Lost</span>3</td>\r\n                <td");

WriteLiteral(" data-stackable=\"true\"");

WriteLiteral("><span");

WriteLiteral(" class=\"table-as-cards__label\"");

WriteLiteral(" aria-hidden=\"true\"");

WriteLiteral(">Tied</span>1</td>\r\n                <td");

WriteLiteral(" data-stackable=\"true\"");

WriteLiteral("><span");

WriteLiteral(" class=\"table-as-cards__label\"");

WriteLiteral(" aria-hidden=\"true\"");

WriteLiteral(">No result</span>0</td>\r\n                <td");

WriteLiteral(" data-stackable=\"true\"");

WriteLiteral("><span");

WriteLiteral(" class=\"table-as-cards__label\"");

WriteLiteral(" aria-hidden=\"true\"");

WriteLiteral(">Runs scored</span>0</td>\r\n                <td");

WriteLiteral(" data-stackable=\"true\"");

WriteLiteral("><span");

WriteLiteral(" class=\"table-as-cards__label\"");

WriteLiteral(" aria-hidden=\"true\"");

WriteLiteral(">Runs conceded</span>0</td>\r\n                <td");

WriteLiteral(" data-stackable=\"true\"");

WriteLiteral("><span");

WriteLiteral(" class=\"table-as-cards__label\"");

WriteLiteral(" aria-hidden=\"true\"");

WriteLiteral(">Points</span>17</td>\r\n            </tr>\r\n            <tr");

WriteLiteral(" class=\"text-muted\"");

WriteLiteral(">\r\n                <th");

WriteLiteral(" scope=\"row\"");

WriteLiteral("><a");

WriteLiteral(" href=\"https://example.org\"");

WriteLiteral(">Team C</a></th>\r\n                <td");

WriteLiteral(" colspan=\"8\"");

WriteLiteral(">Withdrawn from league</td>\r\n            </tr>\r\n        </tbody>\r\n    </table>\r\n " +
"   <h2>Steps in a process</h2>\r\n    <ol");

WriteLiteral(" class=\"steps\"");

WriteLiteral(">\r\n        <li");

WriteLiteral(" class=\"steps__completed\"");

WriteLiteral("><span");

WriteLiteral(" class=\"sr-only\"");

WriteLiteral(">First step</span></li>\r\n        <li");

WriteLiteral(" aria-current=\"step\"");

WriteLiteral("><em");

WriteLiteral(" class=\"sr-only\"");

WriteLiteral(">Second step</em></li>\r\n        <li><span");

WriteLiteral(" class=\"sr-only\"");

WriteLiteral(">Third step</span></li>\r\n        <li><span");

WriteLiteral(" class=\"sr-only\"");

WriteLiteral(">Fourth step</span></li>\r\n        <li><span");

WriteLiteral(" class=\"sr-only\"");

WriteLiteral(">Fifth step</span></li>\r\n        <li><span");

WriteLiteral(" class=\"sr-only\"");

WriteLiteral(">Sixth step</span></li>\r\n    </ol>\r\n    <div");

WriteLiteral(" class=\"alert alert-info\"");

WriteLiteral(">\r\n        <p>A paragraph within an informational alert, including a <a");

WriteLiteral(" href=\"https://example.org\"");

WriteLiteral(">link</a>.</p>\r\n    </div>\r\n    <div");

WriteLiteral(" class=\"alert alert-info\"");

WriteLiteral(">\r\n        <p>Two alerts together.</p>\r\n    </div>\r\n    <h2>Stoolball listings</h" +
"2>\r\n    <p>This is a list of teams.</p>\r\n");

WriteLiteral("    ");

            
            #line 136 "..\..\Views\StyleGuideStoolballData.cshtml"
Write(Html.Partial("_TeamList", new List<Team> {
        new Team { TeamName = "Team A", TeamRoute = "/teams/team-a" },
        new Team { TeamName = "Team B", TeamRoute = "/teams/team-b" }
    }));

            
            #line default
            #line hidden
WriteLiteral("\r\n    <p>This is a list of matches.</p>\r\n");

            
            #line 141 "..\..\Views\StyleGuideStoolballData.cshtml"
    
            
            #line default
            #line hidden
            
            #line 141 "..\..\Views\StyleGuideStoolballData.cshtml"
      
        var matchListing = new MatchListingViewModel(Umbraco.AssignedContentItem, Services.UserService)
        {
            DateTimeFormatter = new Stoolball.Dates.DateTimeFormatter()
        };
        matchListing.MatchTypesToLabel.Add(MatchType.FriendlyMatch);
        matchListing.Matches.AddRange(new[] {
    new MatchListing {
        MatchName = "Team A v Team B",
        MatchRoute = "/matches/team-a-v-team-b",
        MatchType = MatchType.LeagueMatch,
        StartTime = DateTime.UtcNow.AddMonths(-1)
        },
    new MatchListing {
        MatchName = "Team A v Team B",
        MatchRoute = "/matches/team-a-v-team-b",
        MatchType = MatchType.FriendlyMatch,
        StartTime = DateTime.UtcNow
        },
    new MatchListing {
        MatchName = "Team A tournament",
        MatchRoute = "/tournaments/team-a-tournament",
        PlayerType = PlayerType.Mixed,
        SpacesInTournament = 5,
        StartTime = DateTime.UtcNow.AddMonths(1)
    }
    });
    
            
            #line default
            #line hidden
WriteLiteral("\r\n");

WriteLiteral("    ");

            
            #line 169 "..\..\Views\StyleGuideStoolballData.cshtml"
Write(Html.Partial("_MatchList", matchListing));

            
            #line default
            #line hidden
WriteLiteral("\r\n\r\n    <p>This is a list of seasons.</p>\r\n");

            
            #line 172 "..\..\Views\StyleGuideStoolballData.cshtml"
    
            
            #line default
            #line hidden
            
            #line 172 "..\..\Views\StyleGuideStoolballData.cshtml"
      
        var seasonList = new SeasonListViewModel
        {
            ShowCompetitionHeading = true
        };
        var competition = new Competition { CompetitionName = "Example competition" };
        competition.Seasons.Add(new Season { FromYear = 2020, UntilYear = 2020, SeasonRoute = "/competitions/example/2020" });
        competition.Seasons.Add(new Season { FromYear = 2020, UntilYear = 2021, SeasonRoute = "/competitions/example/2020-21" });

        seasonList.Competitions.Add(competition);
    
            
            #line default
            #line hidden
WriteLiteral("\r\n");

WriteLiteral("    ");

            
            #line 183 "..\..\Views\StyleGuideStoolballData.cshtml"
Write(Html.Partial("_SeasonList", seasonList));

            
            #line default
            #line hidden
WriteLiteral("\r\n    <div");

WriteLiteral(" id=\"location-map\"");

WriteLiteral(" data-latitude=\"50.995715487915\"");

WriteLiteral(" data-longitude=\"0.088866949081421\"");

WriteLiteral(" data-precision=\"exact\"");

WriteLiteral(" data-title=\"Maresfield Recreation Ground, Maresfield\"");

WriteLiteral(">\r\n        <p><a");

WriteLiteral(" href=\"https://maps.google.co.uk/?z=16&amp;q=Maresfield+Recreation+Ground%2c+Mare" +
"sfield@50.995715487915,0.088866949081421&amp;ll=50.995715487915,0.08886694908142" +
"1\"");

WriteLiteral(">Map of Maresfield Recreation Ground, Maresfield on Google Maps</a></p>\r\n    </di" +
"v>\r\n    <h2>Scorecards</h2>\r\n");

            
            #line 188 "..\..\Views\StyleGuideStoolballData.cshtml"
    
            
            #line default
            #line hidden
            
            #line 188 "..\..\Views\StyleGuideStoolballData.cshtml"
      
        var innings = new EditScorecardViewModel(Model, Services.UserService)
        {
            Match = new Match
            {
                PlayersPerTeam = 2
            },
            CurrentInnings = new MatchInningsViewModel
            {
                MatchInnings = new MatchInnings
                {
                    BattingTeam = new TeamInMatch { Team = new Team { TeamName = "The batting team" } },
                    BowlingTeam = new TeamInMatch { Team = new Team { TeamName = "The bowling team" } },
                    Byes = 2,
                    Wides = 4,
                    NoBalls = 6,
                    BonusOrPenaltyRuns = 8,
                    Runs = 100,
                    Wickets = 1
                }
            }
        };
        innings.CurrentInnings.MatchInnings.PlayerInnings.Add(new PlayerInnings
        {
            Batter = new PlayerIdentity { PlayerIdentityName = "Player One", Player = new Player { PlayerRoute  ="/players/player-one" } },
            DismissalType = DismissalType.Caught,
            DismissedBy = new PlayerIdentity { PlayerIdentityName = "Player Two", Player = new Player { PlayerRoute = "/players/player-two" } },
            Bowler = new PlayerIdentity { PlayerIdentityName = "Player Three", Player = new Player { PlayerRoute = "/players/player-three" } },
            RunsScored = 50,
            BallsFaced = 60
        });
        innings.CurrentInnings.MatchInnings.PlayerInnings.Add(new PlayerInnings
        {
            Batter = new PlayerIdentity { PlayerIdentityName = "Player Four", Player = new Player { PlayerRoute = "/players/player-four" } },
            DismissalType = DismissalType.BodyBeforeWicket,
            Bowler = new PlayerIdentity { PlayerIdentityName = "Player Three", Player = new Player { PlayerRoute = "/players/player-three" } },
            RunsScored = 10,
            BallsFaced = 10
        });
        innings.CurrentInnings.PlayerInningsSearch.Add(new PlayerInningsViewModel
        {
            Batter = "Player One",
            DismissalType = DismissalType.Caught,
            DismissedBy = "Player Two",
            Bowler = "Player Three",
            RunsScored = 50,
            BallsFaced = 60
        });
        innings.CurrentInnings.PlayerInningsSearch.Add(new PlayerInningsViewModel
        {
            Batter = "Player Four",
            DismissalType = DismissalType.BodyBeforeWicket,
            Bowler = "Player Three",
            RunsScored = 10,
            BallsFaced = 10
        });
        innings.CurrentInnings.MatchInnings.OversBowled.Add(new Over
        {
            Bowler = new PlayerIdentity { PlayerIdentityName = "Player Two", Player = new Player { PlayerRoute = "/players/player-two" } },
            BallsBowled = 8,
            Wides = 2,
            NoBalls = 4,
            RunsConceded = 8
        });
        innings.CurrentInnings.OversBowledSearch.Add(new OverViewModel
        {
            BowledBy = "Player Two",
            BallsBowled = 8,
            Wides = 2,
            NoBalls = 4,
            RunsConceded = 8
        });
        var scorecardViewModel = new ScorecardViewModel { MatchInnings = innings.CurrentInnings.MatchInnings };
        
            
            #line default
            #line hidden
WriteLiteral("\r\n\r\n\r\n");

WriteLiteral("    ");

            
            #line 264 "..\..\Views\StyleGuideStoolballData.cshtml"
Write(Html.Partial("_BattingScorecard", scorecardViewModel));

            
            #line default
            #line hidden
WriteLiteral("\r\n");

WriteLiteral("    ");

            
            #line 265 "..\..\Views\StyleGuideStoolballData.cshtml"
Write(Html.Partial("_BowlingScorecard", scorecardViewModel));

            
            #line default
            #line hidden
WriteLiteral("\r\n");

WriteLiteral("    ");

            
            #line 266 "..\..\Views\StyleGuideStoolballData.cshtml"
Write(Html.Partial("_EditBattingScorecard", innings));

            
            #line default
            #line hidden
WriteLiteral("\r\n");

WriteLiteral("    ");

            
            #line 267 "..\..\Views\StyleGuideStoolballData.cshtml"
Write(Html.Partial("_EditBowlingScorecard", innings));

            
            #line default
            #line hidden
WriteLiteral("\r\n");

            
            #line 268 "..\..\Views\StyleGuideStoolballData.cshtml"
    
            
            #line default
            #line hidden
            
            #line 268 "..\..\Views\StyleGuideStoolballData.cshtml"
       
        var comments = new List<HtmlComment> {
            new HtmlComment
            {
                MemberName = "Member One",
                MemberEmail = "member.one@example.org",
                CommentDate = DateTimeOffset.UtcNow,
                Comment = "<p>This is a comment.</p>"
            },
            new HtmlComment
            {
                MemberName = "Member Two",
                MemberEmail = "member.two@example.org",
                CommentDate = DateTimeOffset.UtcNow.AddDays(-5),
                Comment = "<p>This is a comment <i>with formatting</i>.</p>"
            }
            ,
            new HtmlComment
            {
                MemberName = "Member Three",
                MemberEmail = "member.three@example.org",
                CommentDate = DateTimeOffset.UtcNow.AddDays(-10),
                Comment = "<p>This is a comment with paragraphs.</p><p>This is a comment with paragraphs.</p>"
            }
        };
    
            
            #line default
            #line hidden
WriteLiteral("\r\n");

WriteLiteral("    ");

            
            #line 294 "..\..\Views\StyleGuideStoolballData.cshtml"
Write(Html.Partial("_Comments", comments));

            
            #line default
            #line hidden
WriteLiteral("\r\n</div>");

        }
    }
}
#pragma warning restore 1591