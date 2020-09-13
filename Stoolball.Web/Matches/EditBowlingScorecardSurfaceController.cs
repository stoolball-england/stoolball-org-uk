using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
    public class EditBowlingScorecardSurfaceController : SurfaceController
    {
        private readonly IMatchDataSource _matchDataSource;
        private readonly IMatchRepository _matchRepository;
        private readonly IAuthorizationPolicy<Stoolball.Matches.Match> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateTimeFormatter;
        private readonly IMatchInningsUrlParser _matchInningsUrlParser;

        public EditBowlingScorecardSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, IMatchDataSource matchDataSource,
            IMatchRepository matchRepository, IAuthorizationPolicy<Stoolball.Matches.Match> authorizationPolicy, IDateTimeFormatter dateTimeFormatter, IMatchInningsUrlParser matchInningsUrlParser)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _matchRepository = matchRepository ?? throw new ArgumentNullException(nameof(matchRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateTimeFormatter = dateTimeFormatter ?? throw new ArgumentNullException(nameof(dateTimeFormatter));
            _matchInningsUrlParser = matchInningsUrlParser ?? throw new ArgumentNullException(nameof(matchInningsUrlParser));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<ActionResult> UpdateMatch([Bind(Prefix = "CurrentInnings", Include = "OversBowled")] MatchInnings postedInnings)
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

            // The bowler name is required if any other fields are filled in for an over, and some player identity names are not allowed
            var i = 0;
            var reservedNames = new[] { "WIDES", "NOBALLS", "BYES", "BONUSRUNS" };
            while (Request.Form[$"CurrentInnings.OversBowled[{i}].PlayerIdentity.PlayerIdentityName"] != null)
            {
                if (reservedNames.Contains(Regex.Replace(Request.Form[$"CurrentInnings.OversBowled[{i}].PlayerIdentity.PlayerIdentityName"].ToUpperInvariant(), "[^A-Z]", string.Empty)))
                {
                    ModelState.AddModelError($"CurrentInnings.OversBowled[{i}].PlayerIdentity.PlayerIdentityName", $"{Request.Form[$"CurrentInnings.OversBowled[{i}].PlayerIdentity.PlayerIdentityName"]} is a reserved name. Please use a different name.");
                }

                if (string.IsNullOrWhiteSpace(Request.Form[$"CurrentInnings.OversBowled[{i}].PlayerIdentity.PlayerIdentityName"]) &&
                    (!string.IsNullOrWhiteSpace(Request.Form[$"CurrentInnings.OversBowled[{i}].BallsBowled"]) ||
                    !string.IsNullOrWhiteSpace(Request.Form[$"CurrentInnings.OversBowled[{i}].Wides"]) ||
                    !string.IsNullOrWhiteSpace(Request.Form[$"CurrentInnings.OversBowled[{i}].NoBalls"]) ||
                    !string.IsNullOrWhiteSpace(Request.Form[$"CurrentInnings.OversBowled[{i}].RunsConceded"])))
                {
                    ModelState.AddModelError($"CurrentInnings.OversBowled[{i}].PlayerIdentity.PlayerIdentityName", $"You've added the {(i + 1).Ordinalize()} over. Please name the bowler.");
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
            model.CurrentInnings.OversBowled = postedInnings.OversBowled.Where(x => x.PlayerIdentity.PlayerIdentityName?.Trim().Length > 0).ToList();
            if (!model.CurrentInnings.Overs.HasValue)
            {
                model.CurrentInnings.Overs = model.Match.Tournament != null ? 6 : 12;
            }
            if (model.CurrentInnings.Overs.Value < postedInnings.OversBowled.Count)
            {
                model.CurrentInnings.Overs = postedInnings.OversBowled.Count;
            }

            model.IsAuthorized = _authorizationPolicy.IsAuthorized(beforeUpdate);

            if (model.IsAuthorized[AuthorizedAction.EditMatchResult] && ModelState.IsValid)
            {
                var currentMember = Members.GetCurrentMember();
                await _matchRepository.UpdateBowlingScorecard(model.CurrentInnings, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                // redirect to the next innings or close of play
                if (model.InningsOrderInMatch.Value < model.Match.MatchInnings.Count)
                {
                    return Redirect($"{model.Match.MatchRoute}/edit/innings/{model.InningsOrderInMatch.Value + 1}/batting");
                }
                else
                {
                    return Redirect(model.Match.MatchRoute + "/edit/close-of-play");
                }
            }

            model.Metadata.PageTitle = "Edit " + model.Match.MatchFullName(x => _dateTimeFormatter.FormatDate(x.LocalDateTime, false, false, false));

            while (model.CurrentInnings.OversBowled.Count < model.CurrentInnings.Overs)
            {
                model.CurrentInnings.OversBowled.Add(new Over());
            }

            return View("EditBowlingScorecard", model);
        }
    }
}