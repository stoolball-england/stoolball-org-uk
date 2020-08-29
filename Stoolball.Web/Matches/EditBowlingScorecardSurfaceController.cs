using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Umbraco.Data.Matches;
using Stoolball.Web.Security;
using System;
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
    public class EditBowlingScorecardSurfaceController : SurfaceController
    {
        private readonly IMatchDataSource _matchDataSource;
        private readonly IMatchRepository _matchRepository;
        private readonly IAuthorizationPolicy<Match> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateTimeFormatter;

        public EditBowlingScorecardSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
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

            var model = new EditBowlingScorecardViewModel(CurrentPage)
            {
                Match = postedMatch,
                DateFormatter = _dateTimeFormatter
            };
            model.Match.MatchId = beforeUpdate.MatchId;
            model.Match.StartTime = beforeUpdate.StartTime;
            model.Match.MatchRoute = beforeUpdate.MatchRoute;

            model.IsAuthorized = _authorizationPolicy.IsAuthorized(beforeUpdate, Members);

            if (model.IsAuthorized[AuthorizedAction.EditMatchResult] && ModelState.IsValid)
            {
                var currentMember = Members.GetCurrentMember();
                // await _matchRepository.UpdateBowlingScorecard(model.Match, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                // redirect to the next innings or close of play
                if (Request.RawUrl.Contains("/1/"))
                {
                    return Redirect(model.Match.MatchRoute + "/edit/innings/2/bowling");
                }
                else
                {
                    return Redirect(model.Match.MatchRoute + "/edit/close-of-play");
                }
            }

            model.Match.MatchName = beforeUpdate.MatchName;
            model.Metadata.PageTitle = "Edit " + model.Match.MatchFullName(x => _dateTimeFormatter.FormatDate(x.LocalDateTime, false, false, false));

            return View("EditBowlingScorecard", model);
        }
    }
}