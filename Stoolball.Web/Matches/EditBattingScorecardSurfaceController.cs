using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Humanizer;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Security;
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
        public async Task<ActionResult> UpdateMatch([Bind(Prefix = "CurrentInnings", Include = "PlayerInnings")] MatchInnings postedInnings)
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

                // The batter name is required if any other fields are filled in for an over
                if (string.IsNullOrWhiteSpace(postedInnings.PlayerInnings[i].PlayerIdentity?.PlayerIdentityName) &&
                    (postedInnings.PlayerInnings[i].HowOut.HasValue &&
                    postedInnings.PlayerInnings[i].HowOut != DismissalType.DidNotBat ||
                    !string.IsNullOrWhiteSpace(postedInnings.PlayerInnings[i].DismissedBy?.PlayerIdentityName) ||
                    !string.IsNullOrWhiteSpace(postedInnings.PlayerInnings[i].Bowler?.PlayerIdentityName) ||
                    postedInnings.PlayerInnings[i].RunsScored != null ||
                    postedInnings.PlayerInnings[i].BallsFaced != null))
                {
                    ModelState.AddModelError($"CurrentInnings.PlayerInnings[{i}].PlayerIdentity.PlayerIdentityName", $"You've added details for the {(i + 1).Ordinalize()} batter. Please name the batter.");
                }
                i++;
            }

            var model = new EditScorecardViewModel(CurrentPage, Services.UserService)
            {
                Match = beforeUpdate,
                DateFormatter = _dateTimeFormatter,
                InningsOrderInMatch = _matchInningsUrlParser.ParseInningsOrderInMatchFromUrl(new Uri(Request.RawUrl, UriKind.Relative))
            };
            model.CurrentInnings = model.Match.MatchInnings.Single(x => x.InningsOrderInMatch == model.InningsOrderInMatch);
            model.CurrentInnings.PlayerInnings = postedInnings.PlayerInnings.Where(x => x.PlayerIdentity?.PlayerIdentityName?.Trim().Length > 0).ToList();
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

            return View("EditBattingScorecard", model);
        }
    }
}