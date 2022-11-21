using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Data.Abstractions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Statistics;
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
    public class EditBattingScorecardSurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly IMatchDataSource _matchDataSource;
        private readonly IMatchRepository _matchRepository;
        private readonly IAuthorizationPolicy<Match> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateTimeFormatter;
        private readonly IMatchInningsUrlParser _matchInningsUrlParser;
        private readonly IPlayerInningsScaffolder _playerInningsScaffolder;
        private readonly IBowlingFiguresCalculator _bowlingFiguresCalculator;
        private readonly IPlayerCacheInvalidator _playerCacheClearer;

        public EditBattingScorecardSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberManager memberManager,
            IMatchDataSource matchDataSource, IMatchRepository matchRepository, IAuthorizationPolicy<Match> authorizationPolicy, IDateTimeFormatter dateTimeFormatter,
            IMatchInningsUrlParser matchInningsUrlParser, IPlayerInningsScaffolder playerInningsScaffolder, IBowlingFiguresCalculator bowlingFiguresCalculator,
            IPlayerCacheInvalidator playerCacheClearer)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _matchRepository = matchRepository ?? throw new ArgumentNullException(nameof(matchRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateTimeFormatter = dateTimeFormatter ?? throw new ArgumentNullException(nameof(dateTimeFormatter));
            _matchInningsUrlParser = matchInningsUrlParser ?? throw new ArgumentNullException(nameof(matchInningsUrlParser));
            _playerInningsScaffolder = playerInningsScaffolder ?? throw new ArgumentNullException(nameof(playerInningsScaffolder));
            _bowlingFiguresCalculator = bowlingFiguresCalculator ?? throw new ArgumentNullException(nameof(bowlingFiguresCalculator));
            _playerCacheClearer = playerCacheClearer ?? throw new ArgumentNullException(nameof(playerCacheClearer));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<ActionResult> UpdateMatch([Bind("MatchInnings", "PlayerInningsSearch", Prefix = "CurrentInnings")] MatchInningsViewModel postedData)
        {
            if (postedData is null)
            {
                throw new ArgumentNullException(nameof(postedData));
            }

            var beforeUpdate = await _matchDataSource.ReadMatchByRoute(Request.Path).ConfigureAwait(false);

            // This page is only for matches in the past, or for tournament matches
            if (beforeUpdate.StartTime > DateTime.UtcNow || beforeUpdate.Tournament != null)
            {
                return NotFound();
            }


            if (beforeUpdate.MatchResultType.HasValue && new List<MatchResultType> {
                    MatchResultType.HomeWinByForfeit, MatchResultType.AwayWinByForfeit, MatchResultType.Postponed, MatchResultType.Cancelled
                }.Contains(beforeUpdate.MatchResultType.Value))
            {
                return NotFound();
            }

            var model = new EditScorecardViewModel(CurrentPage, Services.UserService)
            {
                Match = beforeUpdate,
                InningsOrderInMatch = _matchInningsUrlParser.ParseInningsOrderInMatchFromUrl(new Uri(Request.Path, UriKind.Relative)),
                Autofocus = true
            };

            // This page is not for innings which don't exist
            if (!beforeUpdate.MatchInnings.Any(x => x.InningsOrderInMatch == model.InningsOrderInMatch))
            {
                return NotFound();
            }

            model.CurrentInnings.MatchInnings = model.Match.MatchInnings.Single(x => x.InningsOrderInMatch == model.InningsOrderInMatch);
            model.CurrentInnings.MatchInnings.PlayerInnings = postedData.PlayerInningsSearch.Where(x => !ModelState.IsValid || !string.IsNullOrWhiteSpace(x.Batter)).Select(x => new PlayerInnings
            {
                Batter = new PlayerIdentity
                {
                    PlayerIdentityName = x.Batter?.Trim(),
                    Team = model.CurrentInnings.MatchInnings.BattingTeam.Team
                },
                DismissalType = x.DismissalType,
                DismissedBy = string.IsNullOrWhiteSpace(x.DismissedBy) ? null : new PlayerIdentity
                {
                    PlayerIdentityName = x.DismissedBy.Trim(),
                    Team = model.CurrentInnings.MatchInnings.BowlingTeam.Team
                },
                Bowler = string.IsNullOrWhiteSpace(x.Bowler) ? null : new PlayerIdentity
                {
                    PlayerIdentityName = x.Bowler.Trim(),
                    Team = model.CurrentInnings.MatchInnings.BowlingTeam.Team
                },
                RunsScored = x.RunsScored,
                BallsFaced = x.BallsFaced
            }).ToList();
            model.CurrentInnings.MatchInnings.Byes = postedData.MatchInnings.Byes;
            model.CurrentInnings.MatchInnings.Wides = postedData.MatchInnings.Wides;
            model.CurrentInnings.MatchInnings.NoBalls = postedData.MatchInnings.NoBalls;
            model.CurrentInnings.MatchInnings.BonusOrPenaltyRuns = postedData.MatchInnings.BonusOrPenaltyRuns;
            model.CurrentInnings.MatchInnings.Runs = postedData.MatchInnings.Runs;
            model.CurrentInnings.MatchInnings.Wickets = postedData.MatchInnings.Wickets;


            if (!model.Match.PlayersPerTeam.HasValue)
            {
                model.Match.PlayersPerTeam = model.Match.Tournament != null ? 8 : 11;
            }
            if (model.Match.PlayersPerTeam.Value < postedData.MatchInnings.PlayerInnings.Count)
            {
                model.Match.PlayersPerTeam = postedData.MatchInnings.PlayerInnings.Count;
            }
            _playerInningsScaffolder.ScaffoldPlayerInnings(model.CurrentInnings.MatchInnings.PlayerInnings, model.Match.PlayersPerTeam.Value);

            model.CurrentInnings.PlayerInningsSearch.AddRange(model.CurrentInnings.MatchInnings.PlayerInnings.Select(x => new PlayerInningsViewModel
            {
                Batter = x.Batter?.PlayerIdentityName,
                DismissalType = x.DismissalType,
                DismissedBy = x.DismissedBy?.PlayerIdentityName,
                Bowler = x.Bowler?.PlayerIdentityName,
                RunsScored = x.RunsScored,
                BallsFaced = x.BallsFaced
            }));

            model.CurrentInnings.MatchInnings.BowlingFigures = _bowlingFiguresCalculator.CalculateBowlingFigures(model.CurrentInnings.MatchInnings);

            model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(beforeUpdate);

            if (model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditMatchResult] && ModelState.IsValid)
            {
                var currentMember = await _memberManager.GetCurrentMemberAsync();
                await _matchRepository.UpdateBattingScorecard(model.Match, model.CurrentInnings.MatchInnings.MatchInningsId!.Value, currentMember.Key, currentMember.Name).ConfigureAwait(false);
                _playerCacheClearer.InvalidateCacheForTeams(model.CurrentInnings.MatchInnings.BattingTeam.Team, model.CurrentInnings.MatchInnings.BowlingTeam.Team);

                // redirect to the bowling scorecard for this innings
                return Redirect($"{model.Match.MatchRoute}/edit/innings/{model.InningsOrderInMatch!.Value}/bowling");
            }

            model.Metadata.PageTitle = "Edit " + model.Match.MatchFullName(x => _dateTimeFormatter.FormatDate(x, false, false, false));

            while (model.CurrentInnings.MatchInnings.OversBowled.Count < model.CurrentInnings.MatchInnings.OverSets.Sum(x => x.Overs))
            {
                model.CurrentInnings.MatchInnings.OversBowled.Add(new Over());
            }

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

            return View("EditBattingScorecard", model);
        }
    }
}