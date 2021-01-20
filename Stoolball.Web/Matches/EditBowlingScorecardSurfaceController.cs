using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Humanizer;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Teams;
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
        private readonly IAuthorizationPolicy<Match> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateTimeFormatter;
        private readonly IMatchInningsUrlParser _matchInningsUrlParser;
        private readonly IBowlingFiguresCalculator _bowlingFiguresCalculator;

        public EditBowlingScorecardSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, IMatchDataSource matchDataSource,
            IMatchRepository matchRepository, IAuthorizationPolicy<Stoolball.Matches.Match> authorizationPolicy, IDateTimeFormatter dateTimeFormatter,
            IMatchInningsUrlParser matchInningsUrlParser, IBowlingFiguresCalculator bowlingFiguresCalculator)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _matchRepository = matchRepository ?? throw new ArgumentNullException(nameof(matchRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateTimeFormatter = dateTimeFormatter ?? throw new ArgumentNullException(nameof(dateTimeFormatter));
            _matchInningsUrlParser = matchInningsUrlParser ?? throw new ArgumentNullException(nameof(matchInningsUrlParser));
            _bowlingFiguresCalculator = bowlingFiguresCalculator ?? throw new ArgumentNullException(nameof(bowlingFiguresCalculator));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<ActionResult> UpdateMatch([Bind(Prefix = "CurrentInnings", Include = "MatchInnings,OversBowledSearch")] MatchInningsViewModel postedData)
        {
            if (postedData is null)
            {
                throw new ArgumentNullException(nameof(postedData));
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

            // The bowler name is required if any other fields are filled in for an over
            var i = 0;
            foreach (var over in postedData.OversBowledSearch)
            {
                if (string.IsNullOrWhiteSpace(over.Bowler) &&
                    (over.BallsBowled.HasValue ||
                     over.Wides.HasValue ||
                     over.NoBalls.HasValue ||
                     over.RunsConceded.HasValue))
                {
                    ModelState.AddModelError($"CurrentInnings.OversBowledSearch[{i}].Bowler", $"You've added the {(i + 1).Ordinalize(CultureInfo.CurrentCulture)} over. Please name the bowler.");
                }
                i++;
            }

            var model = new EditScorecardViewModel(CurrentPage, Services.UserService)
            {
                Match = beforeUpdate,
                InningsOrderInMatch = _matchInningsUrlParser.ParseInningsOrderInMatchFromUrl(new Uri(Request.RawUrl, UriKind.Relative)),
                DateFormatter = _dateTimeFormatter,
                Autofocus = true
            };
            model.CurrentInnings.MatchInnings = model.Match.MatchInnings.Single(x => x.InningsOrderInMatch == model.InningsOrderInMatch);
            model.CurrentInnings.MatchInnings.OversBowled = postedData.OversBowledSearch.Where(x => x.Bowler?.Trim().Length > 0).Select(x => new Over
            {
                PlayerIdentity = new PlayerIdentity
                {
                    PlayerIdentityName = x.Bowler.Trim(),
                    Team = model.CurrentInnings.MatchInnings.BowlingTeam.Team
                },
                BallsBowled = x.BallsBowled,
                Wides = x.Wides,
                NoBalls = x.NoBalls,
                RunsConceded = x.RunsConceded
            }).ToList();
            model.CurrentInnings.OversBowledSearch = postedData.OversBowledSearch;
            if (!model.CurrentInnings.MatchInnings.Overs.HasValue)
            {
                model.CurrentInnings.MatchInnings.Overs = model.Match.Tournament != null ? 6 : 12;
            }
            if (model.CurrentInnings.MatchInnings.Overs.Value < postedData.OversBowledSearch.Count)
            {
                model.CurrentInnings.MatchInnings.Overs = postedData.OversBowledSearch.Count;
            }

            model.CurrentInnings.MatchInnings.BowlingFigures = _bowlingFiguresCalculator.CalculateBowlingFigures(model.CurrentInnings.MatchInnings);

            model.IsAuthorized = _authorizationPolicy.IsAuthorized(beforeUpdate);

            if (model.IsAuthorized[AuthorizedAction.EditMatchResult] && ModelState.IsValid)
            {
                var currentMember = Members.GetCurrentMember();
                await _matchRepository.UpdateBowlingScorecard(model.CurrentInnings.MatchInnings, currentMember.Key, currentMember.Name).ConfigureAwait(false);

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

            while (model.CurrentInnings.MatchInnings.OversBowled.Count < model.CurrentInnings.MatchInnings.Overs)
            {
                model.CurrentInnings.MatchInnings.OversBowled.Add(new Over());
            }

            model.Breadcrumbs.Add(new Breadcrumb { Name = model.Match.MatchName, Url = new Uri(model.Match.MatchRoute, UriKind.Relative) });

            return View("EditBowlingScorecard", model);
        }
    }
}