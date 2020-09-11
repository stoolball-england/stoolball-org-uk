using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Teams;
using Stoolball.Umbraco.Data.Competitions;
using Stoolball.Umbraco.Data.Matches;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;

namespace Stoolball.Web.Matches
{
    public class EditStartOfPlaySurfaceController : SurfaceController
    {
        private readonly IMatchDataSource _matchDataSource;
        private readonly IMatchRepository _matchRepository;
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly IAuthorizationPolicy<Match> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateTimeFormatter;
        private readonly IEditMatchHelper _editMatchHelper;

        public EditStartOfPlaySurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, IMatchDataSource matchDataSource,
            IMatchRepository matchRepository, ISeasonDataSource seasonDataSource, IAuthorizationPolicy<Match> authorizationPolicy, IDateTimeFormatter dateTimeFormatter,
            IEditMatchHelper editMatchHelper)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _matchRepository = matchRepository ?? throw new ArgumentNullException(nameof(matchRepository));
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateTimeFormatter = dateTimeFormatter ?? throw new ArgumentNullException(nameof(dateTimeFormatter));
            _editMatchHelper = editMatchHelper ?? throw new ArgumentNullException(nameof(editMatchHelper));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<ActionResult> UpdateMatch([Bind(Prefix = "Match", Include = "MatchResultType")] Match postedMatch)
        {
            if (postedMatch is null)
            {
                throw new ArgumentNullException(nameof(postedMatch));
            }

            var beforeUpdate = await _matchDataSource.ReadMatchByRoute(Request.RawUrl).ConfigureAwait(false);

            if (beforeUpdate.StartTime > DateTime.UtcNow)
            {
                return new HttpNotFoundResult();
            }

            var model = new EditStartOfPlayViewModel(CurrentPage)
            {
                Match = postedMatch,
                DateFormatter = _dateTimeFormatter
            };
            model.Match.MatchId = beforeUpdate.MatchId;
            model.Match.MatchType = beforeUpdate.MatchType;
            model.Match.StartTime = beforeUpdate.StartTime;
            model.Match.MatchRoute = beforeUpdate.MatchRoute;
            model.Match.UpdateMatchNameAutomatically = beforeUpdate.UpdateMatchNameAutomatically;
            model.Match.Teams = beforeUpdate.Teams;
            model.Match.MatchInnings = beforeUpdate.MatchInnings;

            await AddMissingTeamsFromRequest(model, beforeUpdate.Season?.SeasonRoute).ConfigureAwait(false);

            ReadMatchLocationFromRequest(model);

            ReadTossWonByFromRequest(model);

            ReadBattedFirstFromRequest(model);

            model.IsAuthorized = _authorizationPolicy.IsAuthorized(beforeUpdate, Members);

            if (model.IsAuthorized[AuthorizedAction.EditMatchResult] && ModelState.IsValid)
            {
                if ((int)model.Match.MatchResultType == -1) { model.Match.MatchResultType = null; }

                var currentMember = Members.GetCurrentMember();
                await _matchRepository.UpdateStartOfPlay(model.Match, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                if (model.Match.MatchResultType.HasValue && new List<MatchResultType> {
                    MatchResultType.HomeWinByForfeit,
                    MatchResultType.AwayWinByForfeit,
                    MatchResultType.Postponed,
                    MatchResultType.Cancelled }.Contains(model.Match.MatchResultType.Value))
                {
                    // There's no scorecard to complete - redirect to the match
                    return Redirect(model.Match.MatchRoute);
                }
                else
                {
                    // Redirect to bowling scorecard for now, but this will go to the batting scorecard
                    return Redirect(model.Match.MatchRoute + "/edit/innings/1/bowling");
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
            model.Metadata.PageTitle = "Edit " + model.Match.MatchFullName(x => _dateTimeFormatter.FormatDate(x.LocalDateTime, false, false, false));

            return View("EditStartOfPlay", model);
        }

        private async Task AddMissingTeamsFromRequest(EditStartOfPlayViewModel model, string seasonRoute)
        {
            if (model.Match.MatchType == MatchType.KnockoutMatch && !string.IsNullOrEmpty(seasonRoute))
            {
                var season = await _seasonDataSource.ReadSeasonByRoute(seasonRoute, true).ConfigureAwait(false);
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
        }

        private void ReadMatchLocationFromRequest(EditStartOfPlayViewModel model)
        {
            if (!string.IsNullOrEmpty(Request.Form["MatchLocationId"]))
            {
                model.MatchLocationId = new Guid(Request.Form["MatchLocationId"]);
                model.MatchLocationName = Request.Form["MatchLocationName"];
                model.Match.MatchLocation = new MatchLocation
                {
                    MatchLocationId = model.MatchLocationId
                };
            }
        }

        private void ReadTossWonByFromRequest(EditStartOfPlayViewModel model)
        {
            model.TossWonBy = Request.Form["TossWonBy"];
            if (!string.IsNullOrEmpty(model.TossWonBy))
            {
                if (Enum.TryParse<TeamRole>(model.TossWonBy, out var tossWonByRole))
                {
                    foreach (var team in model.Match.Teams)
                    {
                        team.WonToss = (team.TeamRole == tossWonByRole);
                    }
                }
                else
                {
                    var tossWonByTeam = Guid.Parse(model.TossWonBy);
                    foreach (var team in model.Match.Teams)
                    {
                        team.WonToss = (team.MatchTeamId == tossWonByTeam);
                    }
                }
            }
            else
            {
                foreach (var team in model.Match.Teams)
                {
                    team.WonToss = null;
                }
            }
        }

        private void ReadBattedFirstFromRequest(EditStartOfPlayViewModel model)
        {
            model.Match.InningsOrderIsKnown = false;
            model.BattedFirst = Request.Form["BattedFirst"];
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
            if (!hasTeamInRole && !string.IsNullOrEmpty(Request.Form[teamRole.ToString() + "TeamId"]))
            {
                teamsInTheMatch.Add(new TeamInMatch
                {
                    Team = new Team
                    {
                        TeamId = new Guid(Request.Form[teamRole.ToString() + "TeamId"]),
                        TeamName = Request.Form[teamRole.ToString() + "TeamName"]
                    },
                    TeamRole = teamRole
                });
            }
        }

        private void AddTeamIfMissing(List<TeamInMatch> teamsInTheMatch, List<TeamInSeason> teamsInTheSeason, TeamRole teamRole)
        {
            var hasTeamInRole = teamsInTheMatch.Any(x => x.TeamRole == teamRole);
            if (!hasTeamInRole && !string.IsNullOrEmpty(Request.Form[teamRole.ToString() + "TeamId"]))
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