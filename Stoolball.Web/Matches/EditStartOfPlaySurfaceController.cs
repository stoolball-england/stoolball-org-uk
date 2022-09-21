using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Website.Controllers;

namespace Stoolball.Web.Matches
{
    public class EditStartOfPlaySurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly IMatchDataSource _matchDataSource;
        private readonly IMatchRepository _matchRepository;
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly IAuthorizationPolicy<Match> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateTimeFormatter;
        private readonly IEditMatchHelper _editMatchHelper;
        private readonly IMatchListingCacheClearer _cacheClearer;

        public EditStartOfPlaySurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberManager memberManager,
            IMatchDataSource matchDataSource, IMatchRepository matchRepository, ISeasonDataSource seasonDataSource,
            IAuthorizationPolicy<Match> authorizationPolicy, IDateTimeFormatter dateTimeFormatter, IEditMatchHelper editMatchHelper, IMatchListingCacheClearer cacheClearer)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _matchRepository = matchRepository ?? throw new ArgumentNullException(nameof(matchRepository));
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateTimeFormatter = dateTimeFormatter ?? throw new ArgumentNullException(nameof(dateTimeFormatter));
            _editMatchHelper = editMatchHelper ?? throw new ArgumentNullException(nameof(editMatchHelper));
            _cacheClearer = cacheClearer ?? throw new ArgumentNullException(nameof(cacheClearer));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<ActionResult> UpdateMatch([Bind("MatchResultType", Prefix = "Match")] Match postedMatch)
        {
            if (postedMatch is null)
            {
                postedMatch = new Match();
            }

            var beforeUpdate = await _matchDataSource.ReadMatchByRoute(Request.Path).ConfigureAwait(false);

            if (beforeUpdate.StartTime > DateTime.UtcNow || beforeUpdate.Tournament != null)
            {
                return NotFound();
            }

            var model = new EditStartOfPlayViewModel(CurrentPage, Services.UserService)
            {
                Match = beforeUpdate
            };
            model.Match.MatchResultType = postedMatch.MatchResultType;

            await AddMissingTeamsFromRequest(model, ModelState);

            ReadMatchLocationFromRequest(model);

            ReadTossWonByFromRequest(model);

            ReadBattedFirstFromRequest(model);

            if (Request.Form.ContainsKey("MatchWentAhead") && bool.TryParse(Request.Form["MatchWentAhead"], out var matchWentAhead))
            {
                // Reset the fields which are not compatible with the selection for whether the match went ahead.
                // They may have been set, then the match went ahead option was changed, and their values would now be misleading.
                model.MatchWentAhead = matchWentAhead;
                if (model.MatchWentAhead.Value && model.Match.MatchResultType != MatchResultType.HomeWin && model.Match.MatchResultType != MatchResultType.AwayWin && model.Match.MatchResultType != MatchResultType.Tie)
                {
                    model.Match.MatchResultType = null;
                }

                if (!model.MatchWentAhead.Value)
                {
                    model.TossWonBy = null;
                    model.BattedFirst = null;
                }

                if (Request.Form.ContainsKey("HasScorecard") && bool.TryParse(Request.Form["HasScorecard"], out var hasScorecard))
                {
                    model.HasScorecard = hasScorecard;
                }

                // Validate this field as required, but conditionally based on whether the match went ahead
                if (!model.MatchWentAhead.Value && !model.Match.MatchResultType.HasValue)
                {
                    ModelState.AddModelError("Match.MatchResultType", "The Why didn't the match go ahead? field is required");
                }
            }

            model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(beforeUpdate);

            if (model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditMatchResult] && ModelState.IsValid)
            {
                var currentMember = await _memberManager.GetCurrentMemberAsync();
                var updatedMatch = await _matchRepository.UpdateStartOfPlay(model.Match, currentMember.Key, currentMember.Name).ConfigureAwait(false);
                await _cacheClearer.ClearCacheFor(updatedMatch).ConfigureAwait(false);

                if (model.Match.MatchResultType.HasValue && new List<MatchResultType> {
                    MatchResultType.HomeWinByForfeit,
                    MatchResultType.AwayWinByForfeit,
                    MatchResultType.Postponed,
                    MatchResultType.Cancelled }.Contains(model.Match.MatchResultType.Value))
                {
                    // There's no scorecard to complete - redirect to the match
                    return Redirect(updatedMatch.MatchRoute);
                }
                else if (model.HasScorecard)
                {
                    // Redirect to batting scorecard
                    return Redirect(updatedMatch.MatchRoute + "/edit/innings/1/batting");
                }
                else
                {
                    // Skip scorecard editing and redirect to close of play
                    return Redirect(updatedMatch.MatchRoute + "/edit/close-of-play");
                }
            }

            // Reset model.Match.Teams to trigger reappearance of fields, but preserve the values selected in model.*TeamId and model.*TeamName
            var selectedHomeTeam = model.Match.Teams.SingleOrDefault(x => !x.MatchTeamId.HasValue && x.TeamRole == TeamRole.Home);
            if (selectedHomeTeam != null)
            {
                model.HomeTeamId = selectedHomeTeam.Team.TeamId;
                model.HomeTeamName = selectedHomeTeam.Team.TeamName;
                model.Match.Teams.Remove(selectedHomeTeam);
            }

            var selectedAwayTeam = model.Match.Teams.SingleOrDefault(x => !x.MatchTeamId.HasValue && x.TeamRole == TeamRole.Away);
            if (selectedAwayTeam != null)
            {
                model.AwayTeamId = selectedAwayTeam.Team.TeamId;
                model.AwayTeamName = selectedAwayTeam.Team.TeamName;
                model.Match.Teams.Remove(selectedAwayTeam);
            }

            model.Match.MatchName = beforeUpdate.MatchName;
            model.Metadata.PageTitle = "Edit " + model.Match.MatchFullName(x => _dateTimeFormatter.FormatDate(x, false, false, false));

            if (model.Match.Season != null)
            {
                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Match.Season.Competition.CompetitionName, Url = new Uri(model.Match.Season.Competition.CompetitionRoute, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Match.Season.SeasonName(), Url = new Uri(model.Match.Season.SeasonRoute, UriKind.Relative) });
            }
            else
            {
                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Matches, Url = new Uri(Constants.Pages.MatchesUrl, UriKind.Relative) });
            }
            model.Breadcrumbs.Add(new Breadcrumb { Name = model.Match.MatchName, Url = new Uri(model.Match.MatchRoute, UriKind.Relative) });

            return View("EditStartOfPlay", model);
        }

        private async Task AddMissingTeamsFromRequest(EditStartOfPlayViewModel model, ModelStateDictionary modelState)
        {
            if (model.Match!.MatchType == MatchType.KnockoutMatch &&
                !string.IsNullOrEmpty(model.Match.Season?.SeasonRoute) &&
                (
                    model.Match.Teams.SingleOrDefault(x => x.TeamRole == TeamRole.Home) == null ||
                    model.Match.Teams.SingleOrDefault(x => x.TeamRole == TeamRole.Away) == null
                ))
            {
                var season = await _seasonDataSource.ReadSeasonByRoute(model.Match.Season?.SeasonRoute, true).ConfigureAwait(false);
                if (season != null)
                {
                    model.PossibleHomeTeams = _editMatchHelper.PossibleTeamsAsListItems(season.Teams);
                    model.PossibleAwayTeams = _editMatchHelper.PossibleTeamsAsListItems(season.Teams);

                    // Add teams to model.Teams only if they're missing and if the posted team is from the same season.
                    // By getting it from the season this also adds the team name, which is used to build the match name.
                    AddTeamIfMissing(model.Match.Teams, season.Teams, TeamRole.Home);
                    AddTeamIfMissing(model.Match.Teams, season.Teams, TeamRole.Away);
                }
            }

            if (model.Match.MatchType == MatchType.FriendlyMatch)
            {
                // Add teams to model.Teams only if they're missing, 
                AddTeamIfMissing(model.Match.Teams, TeamRole.Home);
                AddTeamIfMissing(model.Match.Teams, TeamRole.Away);
            }

            if (model.Match.MatchType == MatchType.KnockoutMatch &&
                model.Match.Teams.Count == 2 &&
                model.Match.Teams[0].Team.TeamId == model.Match.Teams[1].Team.TeamId)
            {
                modelState.AddModelError("AwayTeamId", "The away team cannot be the same as the home team");
            }
        }

        private void ReadMatchLocationFromRequest(EditStartOfPlayViewModel model)
        {
            if (Request.Form.ContainsKey("MatchLocationId") && !string.IsNullOrEmpty(Request.Form["MatchLocationId"]))
            {
                model.MatchLocationId = new Guid(Request.Form["MatchLocationId"]);
                if (Request.Form.ContainsKey("MatchLocationName"))
                {
                    model.MatchLocationName = Request.Form["MatchLocationName"];
                }
                model.Match!.MatchLocation = new MatchLocation
                {
                    MatchLocationId = model.MatchLocationId
                };
            }
        }

        private void ReadTossWonByFromRequest(EditStartOfPlayViewModel model)
        {
            if (Request.Form.ContainsKey("TossWonBy"))
            {
                model.TossWonBy = Request.Form["TossWonBy"];
            }
            if (!string.IsNullOrEmpty(model.TossWonBy))
            {
                if (Enum.TryParse<TeamRole>(model.TossWonBy, out var tossWonByRole))
                {
                    foreach (var team in model.Match!.Teams)
                    {
                        team.WonToss = (team.TeamRole == tossWonByRole);
                    }
                }
                else
                {
                    var tossWonByTeam = Guid.Parse(model.TossWonBy);
                    foreach (var team in model.Match!.Teams)
                    {
                        team.WonToss = (team.MatchTeamId == tossWonByTeam);
                    }
                }
            }
            else
            {
                foreach (var team in model.Match!.Teams)
                {
                    team.WonToss = null;
                }
            }
        }

        private void ReadBattedFirstFromRequest(EditStartOfPlayViewModel model)
        {
            model.Match!.InningsOrderIsKnown = false;
            if (Request.Form.ContainsKey("BattedFirst"))
            {
                model.BattedFirst = Request.Form["BattedFirst"];
            }
            if (!string.IsNullOrEmpty(model.BattedFirst))
            {
                model.Match.InningsOrderIsKnown = true;

                if (Enum.TryParse<TeamRole>(model.BattedFirst, out var roleBattedFirst))
                {
                    foreach (var team in model.Match.Teams)
                    {
                        team.BattedFirst = (team.TeamRole == roleBattedFirst);
                    }
                }
                else
                {
                    var battedFirst = Guid.Parse(model.BattedFirst);
                    foreach (var team in model.Match.Teams)
                    {
                        team.BattedFirst = (team.MatchTeamId == battedFirst);
                    }
                }
            }
            else
            {
                foreach (var team in model.Match.Teams)
                {
                    team.BattedFirst = null;
                }
            }
        }

        private void AddTeamIfMissing(List<TeamInMatch> teamsInTheMatch, TeamRole teamRole)
        {
            var hasTeamInRole = teamsInTheMatch.Any(x => x.TeamRole == teamRole);
            if (!hasTeamInRole && Request.Form.ContainsKey(teamRole.ToString() + "TeamId") && !string.IsNullOrEmpty(Request.Form[teamRole.ToString() + "TeamId"]))
            {
                var teamInMatch = new TeamInMatch
                {
                    Team = new Team
                    {
                        TeamId = new Guid(Request.Form[teamRole.ToString() + "TeamId"])
                    },
                    TeamRole = teamRole
                };
                if (Request.Form.ContainsKey(teamRole.ToString() + "TeamName"))
                {
                    teamInMatch.Team.TeamName = Request.Form[teamRole.ToString() + "TeamName"];
                }
                teamsInTheMatch.Add(teamInMatch);
            }
        }

        private void AddTeamIfMissing(List<TeamInMatch> teamsInTheMatch, List<TeamInSeason> teamsInTheSeason, TeamRole teamRole)
        {
            var hasTeamInRole = teamsInTheMatch.Any(x => x.TeamRole == teamRole);
            if (!hasTeamInRole && Request.Form.ContainsKey(teamRole.ToString() + "TeamId") && !string.IsNullOrEmpty(Request.Form[teamRole.ToString() + "TeamId"]))
            {
                Guid? postedTeamId = new Guid(Request.Form[teamRole.ToString() + "TeamId"]);
                var team = teamsInTheSeason.SingleOrDefault(x => x.Team.TeamId == postedTeamId)?.Team;
                if (team != null)
                {
                    teamsInTheMatch.Add(new TeamInMatch
                    {
                        Team = team,
                        TeamRole = teamRole
                    });
                }
            }
        }
    }
}