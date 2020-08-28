using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Umbraco.Data.Competitions;
using Stoolball.Umbraco.Data.Matches;
using Stoolball.Web.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
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
            model.Match.MatchRoute = beforeUpdate.MatchRoute;
            model.Match.UpdateMatchNameAutomatically = beforeUpdate.UpdateMatchNameAutomatically;
            model.Match.Teams = beforeUpdate.Teams;
            model.Match.MatchInnings = beforeUpdate.MatchInnings;

            if (beforeUpdate.MatchType == MatchType.KnockoutMatch)
            {
                var season = await _seasonDataSource.ReadSeasonByRoute(beforeUpdate.Season.SeasonRoute, true).ConfigureAwait(false);
                if (season != null)
                {
                    model.PossibleHomeTeams = _editMatchHelper.PossibleTeamsAsListItems(season.Teams);
                    model.PossibleAwayTeams = _editMatchHelper.PossibleTeamsAsListItems(season.Teams);

                    // Add teams to model.Teams only if they're missing and if the posted team is from the same season.
                    // By getting it from the season this also adds the team name, which is used to build the match name.
                    model.HomeTeamId = AddTeamIfMissing(model.Match.Teams, season.Teams, TeamRole.Home);
                    model.AwayTeamId = AddTeamIfMissing(model.Match.Teams, season.Teams, TeamRole.Away);
                }
            }

            if (!string.IsNullOrEmpty(Request.Form["MatchLocationId"]))
            {
                model.MatchLocationId = new Guid(Request.Form["MatchLocationId"]);
                model.MatchLocationName = Request.Form["MatchLocationName"];
                model.Match.MatchLocation = new MatchLocation
                {
                    MatchLocationId = model.MatchLocationId
                };
            }

            if (!string.IsNullOrEmpty(Request.Form["TossWonBy"]))
            {
                if (Enum.TryParse<TeamRole>(Request.Form["TossWonBy"], out var tossWonByRole))
                {
                    foreach (var team in model.Match.Teams)
                    {
                        team.WonToss = (team.TeamRole == tossWonByRole);
                    }
                }
                else
                {
                    var tossWonByTeam = Guid.Parse(Request.Form["TossWonBy"]);
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

            model.Match.InningsOrderIsKnown = false;
            if (!string.IsNullOrEmpty(Request.Form["BattedFirst"]))
            {
                model.Match.InningsOrderIsKnown = true;

                if (Enum.TryParse<TeamRole>(Request.Form["BattedFirst"], out var roleBattedFirst))
                {
                    foreach (var team in model.Match.Teams)
                    {
                        team.BattedFirst = (team.TeamRole == roleBattedFirst);
                    }
                }
                else
                {
                    var battedFirst = Guid.Parse(Request.Form["BattedFirst"]);
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
                    // Redirect to the match for now, but this will go to the scorecard
                    return Redirect(model.Match.MatchRoute);
                }
            }
            model.Match.MatchName = beforeUpdate.MatchName;
            model.Metadata.PageTitle = "Edit " + model.Match.MatchFullName(x => _dateTimeFormatter.FormatDate(x.LocalDateTime, false, false, false));

            return View("EditStartOfPlay", model);
        }

        private Guid? AddTeamIfMissing(List<TeamInMatch> teamsInTheMatch, List<TeamInSeason> teamsInTheSeason, TeamRole teamRole)
        {
            Guid? postedTeamId = null;
            var hasTeamInRole = teamsInTheMatch.Any(x => x.TeamRole == teamRole);
            if (!hasTeamInRole && !string.IsNullOrEmpty(Request.Form[teamRole.ToString() + "TeamId"]))
            {
                postedTeamId = new Guid(Request.Form[teamRole.ToString() + "TeamId"]);
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
            return postedTeamId;
        }
    }
}