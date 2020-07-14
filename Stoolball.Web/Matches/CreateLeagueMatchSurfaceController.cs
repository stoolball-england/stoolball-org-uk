using Stoolball.Matches;
using Stoolball.Teams;
using Stoolball.Umbraco.Data.Competitions;
using Stoolball.Umbraco.Data.Matches;
using Stoolball.Umbraco.Data.Teams;
using Stoolball.Web.Security;
using System;
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
    public class CreateLeagueMatchSurfaceController : SurfaceController
    {
        private readonly IMatchRepository _matchRepository;
        private readonly ICreateLeagueMatchEligibleSeasons _createLeagueMatchEligibleSeasons;
        private readonly ITeamDataSource _teamDataSource;
        private readonly ISeasonDataSource _seasonDataSource;

        public CreateLeagueMatchSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, ITeamDataSource teamDataSource, ISeasonDataSource seasonDataSource,
            IMatchRepository matchRepository, ICreateLeagueMatchEligibleSeasons createLeagueMatchEligibleSeasons)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _matchRepository = matchRepository ?? throw new ArgumentNullException(nameof(matchRepository));
            _createLeagueMatchEligibleSeasons = createLeagueMatchEligibleSeasons ?? throw new ArgumentNullException(nameof(createLeagueMatchEligibleSeasons));
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<ActionResult> CreateMatch([Bind(Prefix = "Match", Include = "Season.SeasonId")] Match model)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var viewModel = new CreateMatchViewModel(CurrentPage) { Match = new Match() };
            if (Request.Url.AbsolutePath.StartsWith("/teams/", StringComparison.OrdinalIgnoreCase))
            {
                viewModel.Team = await _teamDataSource.ReadTeamByRoute(Request.RawUrl, true).ConfigureAwait(false);
                viewModel.PossibleSeasons = _createLeagueMatchEligibleSeasons.SelectEligibleSeasons(viewModel.Team?.Seasons)
                    .Select(x => new SelectListItem { Text = x.SeasonFullName(), Value = x.SeasonId.Value.ToString() })
                    .ToList();
            }
            else if (Request.Url.AbsolutePath.StartsWith("/competitions/", StringComparison.OrdinalIgnoreCase))
            {
                viewModel.Season = await _seasonDataSource.ReadSeasonByRoute(Request.RawUrl).ConfigureAwait(false);
            }

            viewModel.IsAuthorized = User.Identity.IsAuthenticated;

            if (viewModel.IsAuthorized && ModelState.IsValid &&
                (viewModel.Team == null || viewModel.PossibleSeasons == null || !viewModel.PossibleSeasons.Any()) &&
                (viewModel.Season == null || !viewModel.Season.MatchTypes.Contains(MatchType.LeagueMatch)))
            {
                var currentMember = Members.GetCurrentMember();
                await _matchRepository.CreateMatch(viewModel.Match, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                // Redirect to the match
                return Redirect(viewModel.Match.MatchRoute);
            }

            if (viewModel.Team != null)
            {
                viewModel.Metadata.PageTitle = $"Add a league match for {viewModel.Team.TeamName}";
            }
            else if (model.Season != null)
            {
                viewModel.Metadata.PageTitle = $"Add a league match in the {viewModel.Season.SeasonFullName()}";
            }
            return View("CreateLeagueMatch", viewModel);
        }
    }
}