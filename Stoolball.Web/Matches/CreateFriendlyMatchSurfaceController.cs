using Humanizer;
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
    public class CreateFriendlyMatchSurfaceController : SurfaceController
    {
        private readonly IMatchRepository _matchRepository;
        private readonly ITeamDataSource _teamDataSource;
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly ICreateMatchSeasonSelector _createMatchSeasonSelector;
        private readonly IEditMatchHelper _editMatchHelper;

        public CreateFriendlyMatchSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, IMatchRepository matchRepository, ITeamDataSource teamDataSource,
            ISeasonDataSource seasonDataSource, ICreateMatchSeasonSelector createMatchSeasonSelector, IEditMatchHelper editMatchHelper)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _matchRepository = matchRepository ?? throw new ArgumentNullException(nameof(matchRepository));
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _seasonDataSource = seasonDataSource;
            _createMatchSeasonSelector = createMatchSeasonSelector;
            _editMatchHelper = editMatchHelper ?? throw new ArgumentNullException(nameof(editMatchHelper));
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

            var model = new EditFriendlyMatchViewModel(CurrentPage) { Match = postedMatch };
            model.Match.MatchType = MatchType.FriendlyMatch;
            if (!model.Match.Season.SeasonId.HasValue)
            {
                model.Match.Season = null;
            }
            _editMatchHelper.ConfigureModelFromRequestData(model, Request.Unvalidated.Form, Request.Form);

            model.IsAuthorized = User.Identity.IsAuthenticated;

            if (model.IsAuthorized && ModelState.IsValid &&
                (model.Season == null || model.Season.MatchTypes.Contains(MatchType.FriendlyMatch)))
            {
                var currentMember = Members.GetCurrentMember();
                await _matchRepository.CreateMatch(model.Match, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                // Redirect to the match
                return Redirect(model.Match.MatchRoute);
            }

            if (Request.RawUrl.StartsWith("/teams/", StringComparison.OrdinalIgnoreCase))
            {
                model.Team = await _teamDataSource.ReadTeamByRoute(Request.RawUrl, true).ConfigureAwait(false);
                var possibleSeasons = _createMatchSeasonSelector.SelectPossibleSeasons(model.Team.Seasons, model.Match.MatchType);
                model.PossibleSeasons = _editMatchHelper.PossibleSeasonsAsListItems(possibleSeasons);
                model.Metadata.PageTitle = $"Add a {MatchType.FriendlyMatch.Humanize(LetterCasing.LowerCase)} for {model.Team.TeamName}";
            }
            else if (Request.RawUrl.StartsWith("/competitions/", StringComparison.OrdinalIgnoreCase))
            {
                model.Match.Season = model.Season = await _seasonDataSource.ReadSeasonByRoute(Request.RawUrl, true).ConfigureAwait(false);
                model.Metadata.PageTitle = $"Add a {MatchType.FriendlyMatch.Humanize(LetterCasing.LowerCase)} in the {model.Season.SeasonFullName()}";
            }

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