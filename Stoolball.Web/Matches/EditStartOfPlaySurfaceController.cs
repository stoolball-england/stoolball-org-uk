using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.MatchLocations;
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
        private readonly IAuthorizationPolicy<Match> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateTimeFormatter;

        public EditStartOfPlaySurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, IMatchDataSource matchDataSource,
            IMatchRepository matchRepository, IAuthorizationPolicy<Match> authorizationPolicy, IDateTimeFormatter dateTimeFormatter)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _matchRepository = matchRepository ?? throw new ArgumentNullException(nameof(matchRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateTimeFormatter = dateTimeFormatter ?? throw new ArgumentNullException(nameof(dateTimeFormatter));
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

            var model = new EditLeagueMatchViewModel(CurrentPage)
            {
                Match = postedMatch,
                DateFormatter = _dateTimeFormatter
            };
            model.Match.MatchId = beforeUpdate.MatchId;
            model.Match.MatchRoute = beforeUpdate.MatchRoute;
            model.Match.UpdateMatchNameAutomatically = beforeUpdate.UpdateMatchNameAutomatically;
            model.Match.Teams = beforeUpdate.Teams;
            model.Match.MatchInnings = beforeUpdate.MatchInnings;

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
                var tossWonBy = Guid.Parse(Request.Form["TossWonBy"]);
                foreach (var team in model.Match.Teams)
                {
                    team.WonToss = (team.MatchTeamId == tossWonBy);
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
                var battedFirst = Guid.Parse(Request.Form["BattedFirst"]);
                var shouldBeOddInnings = model.Match.MatchInnings.Where(x => x.BattingMatchTeamId == battedFirst);
                var shouldBeEvenInnings = model.Match.MatchInnings.Where(x => x.BattingMatchTeamId != battedFirst);
                foreach (var innings in shouldBeOddInnings)
                {
                    var alreadyOdd = ((innings.InningsOrderInMatch % 2) == 1);
                    innings.InningsOrderInMatch = alreadyOdd ? innings.InningsOrderInMatch : innings.InningsOrderInMatch - 1;
                }
                foreach (var innings in shouldBeEvenInnings)
                {
                    var alreadyEven = ((innings.InningsOrderInMatch % 2) == 0);
                    innings.InningsOrderInMatch = alreadyEven ? innings.InningsOrderInMatch : innings.InningsOrderInMatch + 1;
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

            return View("EditMatchResult", model);
        }
    }
}