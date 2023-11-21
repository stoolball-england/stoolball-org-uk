using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Email;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Competitions.Models;
using Stoolball.Web.Navigation;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Competitions
{
    public class SeasonResultsTableController : RenderController, IRenderControllerAsync
    {
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;
        private readonly IEmailProtector _emailProtector;

        public SeasonResultsTableController(ILogger<SeasonResultsTableController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            ISeasonDataSource seasonDataSource,
            IMatchListingDataSource matchDataSource,
            IAuthorizationPolicy<Competition> authorizationPolicy,
            IEmailProtector emailProtector)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _emailProtector = emailProtector ?? throw new ArgumentNullException(nameof(emailProtector));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var model = new SeasonViewModel(CurrentPage)
            {
                Season = await _seasonDataSource.ReadSeasonByRoute(Request.Path, true).ConfigureAwait(false)
            };

            if (model.Season == null || (!model.Season.MatchTypes.Contains(MatchType.LeagueMatch) &&
                !model.Season.MatchTypes.Contains(MatchType.KnockoutMatch) &&
                !model.Season.MatchTypes.Contains(MatchType.FriendlyMatch) &&
                string.IsNullOrEmpty(model.Season.Results)))
            {
                return NotFound();
            }
            else
            {
                var resultsViewModel = new SeasonResultsTableViewModel(CurrentPage)
                {
                    Matches = await _matchDataSource.ReadMatchListings(new MatchFilter
                    {
                        SeasonIds = new List<Guid> { model.Season.SeasonId!.Value },
                        IncludeTournaments = false
                    }, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false)
                };
                model.Matches = resultsViewModel;
                model.Season.PointsRules.AddRange(await _seasonDataSource.ReadPointsRules(model.Season.SeasonId.Value).ConfigureAwait(false));
                model.Season.PointsAdjustments.AddRange(await _seasonDataSource.ReadPointsAdjustments(model.Season.SeasonId.Value).ConfigureAwait(false));

                model.Season.Results = _emailProtector.ProtectEmailAddresses(model.Season.Results, User.Identity?.IsAuthenticated ?? false);

                var resultsData = WorkOutResults(model.Season, model.Matches.Matches);
                resultsViewModel.ResultsTableRows = resultsData.ResultsTableRows;
                resultsViewModel.MatchesAwaitingResults = resultsData.MatchesAwaitingResults;

                model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(model.Season.Competition);

                var the = model.Season.Competition.CompetitionName.StartsWith("THE ", StringComparison.OrdinalIgnoreCase);
                model.Metadata.PageTitle = $"Results table for {(the ? string.Empty : "the ")}{model.Season.SeasonFullNameAndPlayerType()}";
                model.Metadata.Description = model.Season.Description();

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Season.Competition.CompetitionName, Url = new Uri(model.Season.Competition.CompetitionRoute, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }

        private (IEnumerable<ResultsTableRow> ResultsTableRows, IEnumerable<MatchListing> MatchesAwaitingResults) WorkOutResults(Season season, IEnumerable<MatchListing> matches)
        {
            var rows = new Dictionary<Guid, ResultsTableRow>();
            foreach (var team in season.Teams)
            {
                if (!team.WithdrawnDate.HasValue)
                {
                    rows.Add(team.Team.TeamId!.Value, new ResultsTableRow { Team = team.Team });
                }
            }

            var withdrawnTeams = season.Teams.Where(x => x.WithdrawnDate.HasValue);
            var matchesAwaitingResults = new List<MatchListing>();

            // Look at matches to build data for each team
            foreach (var match in matches)
            {
                var homeTeam = match.Teams.FirstOrDefault(team => team.TeamRole == TeamRole.Home);
                var awayTeam = match.Teams.FirstOrDefault(team => team.TeamRole == TeamRole.Away);
                var homeRuns = homeTeam != null ? match.MatchInnings.Where(x => x.BattingMatchTeamId == homeTeam.MatchTeamId).Sum(x => x.Runs) : null;
                var awayRuns = awayTeam != null ? match.MatchInnings.Where(x => x.BattingMatchTeamId == awayTeam.MatchTeamId).Sum(x => x.Runs) : null;

                // Discount matches in the future, unless they've been forfeited
                if (match.StartTime >= DateTime.UtcNow && !match.IsNoResult() && !match.IsForfeit()) { break; }

                // Discount non-league matches
                if (season.ResultsTableType == ResultsTableType.LeagueTable && match.MatchType != MatchType.LeagueMatch) { continue; }

                // Discount postponed matches
                if (match.MatchResultType == MatchResultType.Postponed || match.MatchResultType == MatchResultType.AbandonedDuringPlayAndPostponed) { continue; }

                // Discount matches where a team has withdrawn from the season
                if (homeTeam != null && withdrawnTeams.Any(x => x.Team.TeamId == homeTeam.Team.TeamId)) { continue; }
                if (awayTeam != null && withdrawnTeams.Any(x => x.Team.TeamId == awayTeam.Team.TeamId)) { continue; }

                // Make a note of missing results, to excuse inaccuracies
                if (!match.MatchResultType.HasValue)
                {
                    matchesAwaitingResults.Add(match);
                    continue;
                }

                // Home team
                if (homeTeam != null && rows.ContainsKey(homeTeam.Team.TeamId!.Value))
                {
                    rows[homeTeam.Team.TeamId.Value].Played++;
                    if (match.IsHomeWin()) { rows[homeTeam.Team.TeamId.Value].Won++; }
                    else if (match.IsAwayWin()) { rows[homeTeam.Team.TeamId.Value].Lost++; }
                    else if (match.IsEqualResult()) { rows[homeTeam.Team.TeamId.Value].Tied++; }
                    else if (match.IsNoResult()) { rows[homeTeam.Team.TeamId.Value].NoResult++; }
                    if (season.EnableRunsScored && homeRuns.HasValue) { rows[homeTeam.Team.TeamId.Value].RunsScored = (rows[homeTeam.Team.TeamId.Value].RunsScored + homeRuns.Value); }
                    if (season.EnableRunsConceded && awayRuns.HasValue) { rows[homeTeam.Team.TeamId.Value].RunsConceded = (rows[homeTeam.Team.TeamId.Value].RunsConceded + awayRuns.Value); }
                    if (season.ResultsTableType == ResultsTableType.LeagueTable)
                    {
                        rows[homeTeam.Team.TeamId.Value].Points = (rows[homeTeam.Team.TeamId.Value].Points + (season.PointsRules.First(x => x.MatchResultType == match.MatchResultType)?.HomePoints ?? 0));
                    }
                }

                // Away team
                if (awayTeam != null && rows.ContainsKey(awayTeam.Team.TeamId!.Value))
                {
                    rows[awayTeam.Team.TeamId.Value].Played++;
                    if (match.IsHomeWin()) { rows[awayTeam.Team.TeamId.Value].Lost++; }
                    else if (match.IsAwayWin()) { rows[awayTeam.Team.TeamId.Value].Won++; }
                    else if (match.IsEqualResult()) { rows[awayTeam.Team.TeamId.Value].Tied++; }
                    else if (match.IsNoResult()) { rows[awayTeam.Team.TeamId.Value].NoResult++; }
                    if (season.EnableRunsScored && awayRuns.HasValue) { rows[awayTeam.Team.TeamId.Value].RunsScored = (rows[awayTeam.Team.TeamId.Value].RunsScored + awayRuns.Value); }
                    if (season.EnableRunsConceded && homeRuns.HasValue) { rows[awayTeam.Team.TeamId.Value].RunsConceded = (rows[awayTeam.Team.TeamId.Value].RunsConceded + homeRuns.Value); }
                    if (season.ResultsTableType == ResultsTableType.LeagueTable)
                    {
                        rows[awayTeam.Team.TeamId.Value].Points = (rows[awayTeam.Team.TeamId.Value].Points + (season.PointsRules.FirstOrDefault(x => x.MatchResultType == match.MatchResultType)?.AwayPoints ?? 0));
                    }
                }
            }

            // Apply points adjustments
            if (season.ResultsTableType == ResultsTableType.LeagueTable)
            {
                foreach (var adjustment in season.PointsAdjustments)
                {
                    if (adjustment.Team != null && adjustment.Points.HasValue)
                    {
                        rows[adjustment.Team.TeamId!.Value].Points += adjustment.Points.Value;
                    }
                }
            }

            return (rows.Values, matchesAwaitingResults);
        }
    }
}