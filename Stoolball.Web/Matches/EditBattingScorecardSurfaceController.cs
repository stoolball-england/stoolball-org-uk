using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
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

        public EditBattingScorecardSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberManager memberManager,
            IMatchDataSource matchDataSource, IMatchRepository matchRepository, IAuthorizationPolicy<Match> authorizationPolicy, IDateTimeFormatter dateTimeFormatter,
            IMatchInningsUrlParser matchInningsUrlParser, IPlayerInningsScaffolder playerInningsScaffolder, IBowlingFiguresCalculator bowlingFiguresCalculator)
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

            var beforeUpdate = await _matchDataSource.ReadMatchByRoute(Request.Path);

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

            var i = 0;
            foreach (var innings in postedData.PlayerInningsSearch)
            {
                // The batter name is required if any other fields are filled in for an innings
                if (string.IsNullOrWhiteSpace(innings.Batter) &&
                    (innings.DismissalType.HasValue &&
                    innings.DismissalType != DismissalType.DidNotBat ||
                    !string.IsNullOrWhiteSpace(innings.DismissedBy) ||
                    !string.IsNullOrWhiteSpace(innings.Bowler) ||
                    innings.RunsScored != null ||
                    innings.BallsFaced != null))
                {
                    ModelState.AddModelError($"CurrentInnings.PlayerInningsSearch[{i}].Batter", $"You've added details for the {(i + 1).Ordinalize(CultureInfo.CurrentCulture)} batter. Please name the batter.");
                }

                // The batter must have batted if any other fields are filled in for an innings
                if ((innings.DismissalType == DismissalType.DidNotBat || innings.DismissalType == DismissalType.TimedOut) &&
                    (!string.IsNullOrWhiteSpace(innings.DismissedBy) ||
                    !string.IsNullOrWhiteSpace(innings.Bowler) ||
                    innings.RunsScored != null ||
                    innings.BallsFaced != null))
                {
                    ModelState.AddModelError($"CurrentInnings.PlayerInningsSearch[{i}].DismissalType", $"You've said the {(i + 1).Ordinalize(CultureInfo.CurrentCulture)} batter did not bat, but you added batting details.");
                }

                // The batter can't be not out if a a bowler or fielder is named
                if ((innings.DismissalType == DismissalType.NotOut || innings.DismissalType == DismissalType.Retired || innings.DismissalType == DismissalType.RetiredHurt) &&
                    (!string.IsNullOrWhiteSpace(innings.DismissedBy) ||
                    !string.IsNullOrWhiteSpace(innings.Bowler)
                    ))
                {
                    ModelState.AddModelError($"CurrentInnings.PlayerInningsSearch[{i}].DismissalType", $"You've said the {(i + 1).Ordinalize(CultureInfo.CurrentCulture)} batter was not out, but you named a fielder and/or bowler.");
                }

                // Caught and bowled by the same person is caught and bowled
                if (innings.DismissalType == DismissalType.Caught &&
                    !string.IsNullOrWhiteSpace(innings.DismissedBy) &&
                    innings.DismissedBy?.Trim() == innings.Bowler?.Trim())
                {
                    innings.DismissalType = DismissalType.CaughtAndBowled;
                    innings.DismissedBy = null;
                }

                // If there's a fielder, the dismissal type should be caught or run-out
                if (innings.DismissalType != DismissalType.Caught &&
                    innings.DismissalType != DismissalType.RunOut &&
                    !string.IsNullOrWhiteSpace(innings.DismissedBy))
                {
                    ModelState.AddModelError($"CurrentInnings.PlayerInningsSearch[{i}].DismissalType", $"You've named the fielder for the {(i + 1).Ordinalize(CultureInfo.CurrentCulture)} batter, but they were not caught or run-out.");
                }

                i++;
            }

            var model = new EditScorecardViewModel(CurrentPage, Services.UserService)
            {
                Match = beforeUpdate,
                InningsOrderInMatch = _matchInningsUrlParser.ParseInningsOrderInMatchFromUrl(new Uri(Request.Path, UriKind.Relative)),
                Autofocus = true
            };
            model.CurrentInnings.MatchInnings = model.Match.MatchInnings.Single(x => x.InningsOrderInMatch == model.InningsOrderInMatch);
            model.CurrentInnings.MatchInnings.PlayerInnings = postedData.PlayerInningsSearch.Where(x => !string.IsNullOrWhiteSpace(x.Batter)).Select(x => new PlayerInnings
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
            model.CurrentInnings.PlayerInningsSearch = postedData.PlayerInningsSearch;
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

            model.CurrentInnings.MatchInnings.BowlingFigures = _bowlingFiguresCalculator.CalculateBowlingFigures(model.CurrentInnings.MatchInnings);

            model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(beforeUpdate);

            if (model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditMatchResult] && ModelState.IsValid)
            {
                var currentMember = await _memberManager.GetCurrentMemberAsync();
                await _matchRepository.UpdateBattingScorecard(model.Match, model.CurrentInnings.MatchInnings.MatchInningsId!.Value, currentMember.Key, currentMember.Name);

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