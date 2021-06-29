using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Humanizer;
using Stoolball.Caching;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
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
    public class CreateLeagueMatchSurfaceController : SurfaceController
    {
        private readonly IMatchRepository _matchRepository;
        private readonly ITeamDataSource _teamDataSource;
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly ICreateMatchSeasonSelector _createMatchSeasonSelector;
        private readonly IEditMatchHelper _editMatchHelper;
        private readonly IMatchValidator _matchValidator;
        private readonly ICacheClearer<Match> _cacheClearer;

        public CreateLeagueMatchSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, IMatchRepository matchRepository, ITeamDataSource teamDataSource,
            ISeasonDataSource seasonDataSource, ICreateMatchSeasonSelector createMatchSeasonSelector, IEditMatchHelper editMatchHelper, IMatchValidator matchValidator,
            ICacheClearer<Match> cacheClearer)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _matchRepository = matchRepository ?? throw new ArgumentNullException(nameof(matchRepository));
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _createMatchSeasonSelector = createMatchSeasonSelector ?? throw new ArgumentNullException(nameof(createMatchSeasonSelector));
            _editMatchHelper = editMatchHelper ?? throw new ArgumentNullException(nameof(editMatchHelper));
            _matchValidator = matchValidator ?? throw new ArgumentNullException(nameof(matchValidator));
            _cacheClearer = cacheClearer ?? throw new ArgumentNullException(nameof(cacheClearer));
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

            var model = new EditLeagueMatchViewModel(CurrentPage, Services.UserService) { Match = postedMatch };
            model.Match.MatchType = MatchType.LeagueMatch;
            _editMatchHelper.ConfigureModelFromRequestData(model, Request.Unvalidated.Form, Request.Form, ModelState);

            if (Request.RawUrl.StartsWith("/competitions/", StringComparison.OrdinalIgnoreCase))
            {
                model.Match.Season = model.Season = await _seasonDataSource.ReadSeasonByRoute(Request.RawUrl, true).ConfigureAwait(false);
            }
            else if (model.Match.Season != null && model.Match.Season.SeasonId.HasValue)
            {
                // Get the season, to support validation against season dates
                model.Match.Season = await _seasonDataSource.ReadSeasonById(model.Match.Season.SeasonId.Value).ConfigureAwait(false);
            }

            _matchValidator.DateIsValidForSqlServer(() => model.MatchDate, ModelState, "MatchDate", "match");
            _matchValidator.DateIsWithinTheSeason(() => model.MatchDate, model.Match.Season, ModelState, "MatchDate", "match");
            _matchValidator.TeamsMustBeDifferent(model, ModelState);

            model.IsAuthorized[AuthorizedAction.CreateMatch] = User.Identity.IsAuthenticated;

            if (model.IsAuthorized[AuthorizedAction.CreateMatch] && ModelState.IsValid &&
                (model.Team == null || (model.PossibleSeasons != null && model.PossibleSeasons.Any())) &&
                (model.Season == null || model.Season.MatchTypes.Contains(MatchType.LeagueMatch)))
            {
                var currentMember = Members.GetCurrentMember();
                var createdMatch = await _matchRepository.CreateMatch(model.Match, currentMember.Key, currentMember.Name).ConfigureAwait(false);
                await _cacheClearer.ClearCacheFor(createdMatch).ConfigureAwait(false);

                return Redirect(createdMatch.MatchRoute);
            }

            if (Request.RawUrl.StartsWith("/teams/", StringComparison.OrdinalIgnoreCase))
            {
                model.Team = await _teamDataSource.ReadTeamByRoute(Request.RawUrl, true).ConfigureAwait(false);
                var possibleSeasons = _createMatchSeasonSelector.SelectPossibleSeasons(model.Team.Seasons, model.Match.MatchType).ToList();
                if (possibleSeasons.Count == 1)
                {
                    model.Match.Season = possibleSeasons.First();
                }
                model.PossibleSeasons = _editMatchHelper.PossibleSeasonsAsListItems(possibleSeasons);
                await _editMatchHelper.ConfigureModelPossibleTeams(model, possibleSeasons).ConfigureAwait(false);
                model.Metadata.PageTitle = $"Add a {MatchType.LeagueMatch.Humanize(LetterCasing.LowerCase)} for {model.Team.TeamName}";
            }
            else
            {
                model.PossibleSeasons = _editMatchHelper.PossibleSeasonsAsListItems(new[] { model.Match.Season });
                model.PossibleHomeTeams = _editMatchHelper.PossibleTeamsAsListItems(model.Season?.Teams);
                model.PossibleAwayTeams = _editMatchHelper.PossibleTeamsAsListItems(model.Season?.Teams);
                model.Metadata.PageTitle = $"Add a {MatchType.LeagueMatch.Humanize(LetterCasing.LowerCase)} in the {model.Season.SeasonFullName()}";
            }

            model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Matches, Url = new Uri(Constants.Pages.MatchesUrl, UriKind.Relative) });
            return View("CreateLeagueMatch", model);
        }

    }
}