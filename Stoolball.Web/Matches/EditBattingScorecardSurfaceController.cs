using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Humanizer;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;

namespace Stoolball.Web.Matches
{
    public class EditBattingScorecardSurfaceController : SurfaceController
    {
        private readonly IMatchDataSource _matchDataSource;
        private readonly IMatchRepository _matchRepository;
        private readonly IAuthorizationPolicy<Stoolball.Matches.Match> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateTimeFormatter;
        private readonly IMatchInningsUrlParser _matchInningsUrlParser;
        private readonly IPlayerInningsScaffolder _playerInningsScaffolder;

        public EditBattingScorecardSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, IMatchDataSource matchDataSource, IMatchRepository matchRepository,
            IAuthorizationPolicy<Stoolball.Matches.Match> authorizationPolicy, IDateTimeFormatter dateTimeFormatter, IMatchInningsUrlParser matchInningsUrlParser, IPlayerInningsScaffolder playerInningsScaffolder)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _matchRepository = matchRepository ?? throw new ArgumentNullException(nameof(matchRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateTimeFormatter = dateTimeFormatter ?? throw new ArgumentNullException(nameof(dateTimeFormatter));
            _matchInningsUrlParser = matchInningsUrlParser ?? throw new ArgumentNullException(nameof(matchInningsUrlParser));
            _playerInningsScaffolder = playerInningsScaffolder ?? throw new ArgumentNullException(nameof(playerInningsScaffolder));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<ActionResult> UpdateMatch([Bind(Prefix = "CurrentInnings", Include = "Byes,Wides,NoBalls,BonusOrPenaltyRuns,Runs,Wickets,PlayerInnings")] MatchInnings postedInnings)
        {
            if (postedInnings is null)
            {
                throw new ArgumentNullException(nameof(postedInnings));
            }

            var beforeUpdate = await _matchDataSource.ReadMatchByRoute(Request.RawUrl).ConfigureAwait(false);

            if (beforeUpdate.StartTime > DateTime.UtcNow)
            {
                return new HttpNotFoundResult();
            }

            if (beforeUpdate.MatchResultType.HasValue && new List<MatchResultType> {
                    MatchResultType.HomeWinByForfeit, MatchResultType.AwayWinByForfeit, MatchResultType.Postponed, MatchResultType.Cancelled
                }.Contains(beforeUpdate.MatchResultType.Value))
            {
                return new HttpNotFoundResult();
            }

            var i = 0;
            foreach (var innings in postedInnings.PlayerInnings)
            {
                // Remove bowling team members if an empty name is posted
                if (string.IsNullOrWhiteSpace(innings.DismissedBy?.PlayerIdentityName)) { innings.DismissedBy = null; }
                if (string.IsNullOrWhiteSpace(innings.Bowler?.PlayerIdentityName)) { innings.Bowler = null; }

                // The batter name is required if any other fields are filled in for an innings
                if (string.IsNullOrWhiteSpace(postedInnings.PlayerInnings[i].PlayerIdentity?.PlayerIdentityName) &&
                    (postedInnings.PlayerInnings[i].DismissalType.HasValue &&
                    postedInnings.PlayerInnings[i].DismissalType != DismissalType.DidNotBat ||
                    !string.IsNullOrWhiteSpace(postedInnings.PlayerInnings[i].DismissedBy?.PlayerIdentityName) ||
                    !string.IsNullOrWhiteSpace(postedInnings.PlayerInnings[i].Bowler?.PlayerIdentityName) ||
                    postedInnings.PlayerInnings[i].RunsScored != null ||
                    postedInnings.PlayerInnings[i].BallsFaced != null))
                {
                    ModelState.AddModelError($"CurrentInnings.PlayerInnings[{i}].PlayerIdentity.PlayerIdentityName", $"You've added details for the {(i + 1).Ordinalize()} batter. Please name the batter.");
                }

                // The batter must have batted if any other fields are filled in for an innings
                if ((postedInnings.PlayerInnings[i].DismissalType == DismissalType.DidNotBat || postedInnings.PlayerInnings[i].DismissalType == DismissalType.TimedOut) &&
                    (!string.IsNullOrWhiteSpace(postedInnings.PlayerInnings[i].DismissedBy?.PlayerIdentityName) ||
                    !string.IsNullOrWhiteSpace(postedInnings.PlayerInnings[i].Bowler?.PlayerIdentityName) ||
                    postedInnings.PlayerInnings[i].RunsScored != null ||
                    postedInnings.PlayerInnings[i].BallsFaced != null))
                {
                    ModelState.AddModelError($"CurrentInnings.PlayerInnings[{i}].DismissalType", $"You've said the {(i + 1).Ordinalize()} batter did not bat, but you added batting details.");
                }

                // The batter can't be not out if a a bowler or fielder is named
                if ((postedInnings.PlayerInnings[i].DismissalType == DismissalType.NotOut || postedInnings.PlayerInnings[i].DismissalType == DismissalType.Retired || postedInnings.PlayerInnings[i].DismissalType == DismissalType.RetiredHurt) &&
                    (!string.IsNullOrWhiteSpace(postedInnings.PlayerInnings[i].DismissedBy?.PlayerIdentityName) ||
                    !string.IsNullOrWhiteSpace(postedInnings.PlayerInnings[i].Bowler?.PlayerIdentityName)
                    ))
                {
                    ModelState.AddModelError($"CurrentInnings.PlayerInnings[{i}].DismissalType", $"You've said the {(i + 1).Ordinalize()} batter was not out, but you named a fielder and/or bowler.");
                }

                // Caught and bowled by the same person is caught and bowled
                if (postedInnings.PlayerInnings[i].DismissalType == DismissalType.Caught &&
                    !string.IsNullOrWhiteSpace(postedInnings.PlayerInnings[i].DismissedBy?.PlayerIdentityName) &&
                    postedInnings.PlayerInnings[i].DismissedBy?.PlayerIdentityName == postedInnings.PlayerInnings[i].Bowler?.PlayerIdentityName)
                {
                    postedInnings.PlayerInnings[i].DismissalType = DismissalType.CaughtAndBowled;
                    postedInnings.PlayerInnings[i].DismissedBy = null;
                }

                // If there's a fielder, the dismissal type should be caught or run-out
                if (postedInnings.PlayerInnings[i].DismissalType != DismissalType.Caught &&
                    postedInnings.PlayerInnings[i].DismissalType != DismissalType.RunOut &&
                    !string.IsNullOrWhiteSpace(postedInnings.PlayerInnings[i].DismissedBy?.PlayerIdentityName))
                {
                    ModelState.AddModelError($"CurrentInnings.PlayerInnings[{i}].DismissalType", $"You've named the fielder for the {(i + 1).Ordinalize()} batter, but they were not caught or run-out.");
                }

                i++;
            }

            var model = new EditScorecardViewModel(CurrentPage, Services.UserService)
            {
                Match = beforeUpdate,
                DateFormatter = _dateTimeFormatter,
                InningsOrderInMatch = _matchInningsUrlParser.ParseInningsOrderInMatchFromUrl(new Uri(Request.RawUrl, UriKind.Relative)),
                Autofocus = true
            };
            model.CurrentInnings = model.Match.MatchInnings.Single(x => x.InningsOrderInMatch == model.InningsOrderInMatch);
            model.CurrentInnings.PlayerInnings = postedInnings.PlayerInnings.Where(x => x.PlayerIdentity?.PlayerIdentityName?.Trim().Length > 0).ToList();
            model.CurrentInnings.Byes = postedInnings.Byes;
            model.CurrentInnings.Wides = postedInnings.Wides;
            model.CurrentInnings.NoBalls = postedInnings.NoBalls;
            model.CurrentInnings.BonusOrPenaltyRuns = postedInnings.BonusOrPenaltyRuns;
            model.CurrentInnings.Runs = postedInnings.Runs;
            model.CurrentInnings.Wickets = postedInnings.Wickets;

            if (!model.Match.PlayersPerTeam.HasValue)
            {
                model.Match.PlayersPerTeam = model.Match.Tournament != null ? 8 : 11;
            }
            if (model.Match.PlayersPerTeam.Value < postedInnings.PlayerInnings.Count)
            {
                model.Match.PlayersPerTeam = postedInnings.PlayerInnings.Count;
            }
            _playerInningsScaffolder.ScaffoldPlayerInnings(model.CurrentInnings.PlayerInnings, model.Match.PlayersPerTeam.Value);

            model.IsAuthorized = _authorizationPolicy.IsAuthorized(beforeUpdate);

            if (model.IsAuthorized[AuthorizedAction.EditMatchResult] && ModelState.IsValid)
            {
                var currentMember = Members.GetCurrentMember();
                await _matchRepository.UpdateBattingScorecard(model.CurrentInnings, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                // redirect to the bowling scorecard for this innings
                return Redirect($"{model.Match.MatchRoute}/edit/innings/{model.InningsOrderInMatch.Value}/bowling");
            }

            model.Metadata.PageTitle = "Edit " + model.Match.MatchFullName(x => _dateTimeFormatter.FormatDate(x.LocalDateTime, false, false, false));

            while (model.CurrentInnings.OversBowled.Count < model.CurrentInnings.Overs)
            {
                model.CurrentInnings.OversBowled.Add(new Over());
            }

            model.Breadcrumbs.Add(new Breadcrumb { Name = model.Match.MatchName, Url = new Uri(model.Match.MatchRoute, UriKind.Relative) });

            return View("EditBattingScorecard", model);
        }
    }
}