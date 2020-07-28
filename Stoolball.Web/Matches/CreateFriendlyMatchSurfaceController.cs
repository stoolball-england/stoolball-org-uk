using Stoolball.Matches;
using Stoolball.Teams;
using Stoolball.Umbraco.Data.Competitions;
using Stoolball.Umbraco.Data.Matches;
using Stoolball.Umbraco.Data.Teams;
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
    public class CreateFriendlyMatchSurfaceController : BaseCreateMatchSurfaceController
    {
        private readonly IMatchRepository _matchRepository;

        public CreateFriendlyMatchSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, ITeamDataSource teamDataSource, ISeasonDataSource seasonDataSource,
            IMatchRepository matchRepository, ICreateMatchSeasonSelector createMatchSeasonSelector)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper, teamDataSource, seasonDataSource,
                  createMatchSeasonSelector)
        {
            _matchRepository = matchRepository ?? throw new ArgumentNullException(nameof(matchRepository));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true, TinyMCE = true)]
        public async Task<ActionResult> CreateMatch([Bind(Prefix = "Match", Include = "Season")] Match postedMatch)
        {
            if (postedMatch is null)
            {
                throw new ArgumentNullException(nameof(postedMatch));
            }

            var model = new CreateFriendlyMatchViewModel(CurrentPage) { Match = postedMatch };
            model.Match.MatchType = MatchType.FriendlyMatch;
            if (!model.Match.Season.SeasonId.HasValue)
            {
                model.Match.Season = null;
            }
            ConfigureModelFromRequestData(model, postedMatch);

            model.IsAuthorized = User.Identity.IsAuthenticated;

            if (model.IsAuthorized && ModelState.IsValid &&
                (model.Season == null || model.Season.MatchTypes.Contains(MatchType.FriendlyMatch)))
            {
                var currentMember = Members.GetCurrentMember();
                await _matchRepository.CreateMatch(model.Match, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                // Redirect to the match
                return Redirect(model.Match.MatchRoute);
            }

            await ConfigureModelForRedisplay(model, MatchType.FriendlyMatch, false).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(Request.Form["HomeTeamName"]))
            {
                model.HomeTeamName = Request.Form["HomeTeamName"];
            }
            if (!string.IsNullOrEmpty(Request.Form["AwayTeamName"]))
            {
                model.AwayTeamName = Request.Form["AwayTeamName"];
            }

            return View("CreateFriendlyMatch", model);
        }
    }
}