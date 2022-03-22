using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Competitions;
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

namespace Stoolball.Web.Competitions
{
    public class CreateSeasonSurfaceController : SurfaceController
    {
        private readonly ICompetitionDataSource _competitionDataSource;
        private readonly ISeasonRepository _seasonRepository;
        private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;

        public CreateSeasonSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, ICompetitionDataSource competitionDataSource,
            ISeasonRepository seasonRepository, IAuthorizationPolicy<Competition> authorizationPolicy)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _competitionDataSource = competitionDataSource ?? throw new ArgumentNullException(nameof(competitionDataSource));
            _seasonRepository = seasonRepository ?? throw new System.ArgumentNullException(nameof(seasonRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(TinyMCE = true, Forms = true)]
        public async Task<ActionResult> CreateSeason([Bind(Prefix = "Season", Include = "FromYear,UntilYear,EnableTournaments,EnableBonusOrPenaltyRuns,PlayersPerTeam,DefaultOverSets,EnableLastPlayerBatsOn")] Season season)
        {
            if (season is null)
            {
                throw new System.ArgumentNullException(nameof(season));
            }

            season.DefaultOverSets.RemoveAll(x => !x.Overs.HasValue);

            // end year is actually populated with the number of years to add to the start year,
            // because that allows the start year to be changed from the default without using JavaScript 
            // to update the value of the end year radio buttons
            season.UntilYear = season.FromYear + season.UntilYear;

            // get this from the unvalidated form instead of via modelbinding so that HTML can be allowed
            season.Introduction = Request.Unvalidated.Form["Season.Introduction"];
            season.Results = Request.Unvalidated.Form["Season.Results"];

            try
            {
                // parse this because there's no way to get it via the standard modelbinder without requiring JavaScript to change the field names on submit
                season.MatchTypes = Request.Form["Season.MatchTypes"]?.Split(',').Select(x => (MatchType)Enum.Parse(typeof(MatchType), x)).ToList() ?? new List<MatchType>();
            }
            catch (InvalidCastException)
            {
                return new HttpStatusCodeResult(400);
            }

            if (!season.MatchTypes.Any() && !season.EnableTournaments)
            {
                ModelState.AddModelError("Season.MatchTypes", $"Please select at least one type of match");
            }

            season.Competition = await _competitionDataSource.ReadCompetitionByRoute(Request.RawUrl).ConfigureAwait(false);

            // Ensure there isn't already a season with the submitted year(s)
            if (season.Competition.Seasons.Any(x => x.FromYear == season.FromYear && x.UntilYear == season.UntilYear))
            {
                ModelState.AddModelError("Season.FromYear", $"There is already a {season.SeasonName()}");
            }

            var isAuthorized = _authorizationPolicy.IsAuthorized(season.Competition);

            if (isAuthorized[AuthorizedAction.EditCompetition] && ModelState.IsValid)
            {
                // Create the season
                var currentMember = Members.GetCurrentMember();
                var createdSeason = await _seasonRepository.CreateSeason(season, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                // Redirect to the season
                return Redirect(createdSeason.SeasonRoute);
            }

            var viewModel = new SeasonViewModel(CurrentPage, Services.UserService)
            {
                Season = season,
            };
            viewModel.IsAuthorized = isAuthorized;
            var the = season.Competition.CompetitionName.StartsWith("THE ", StringComparison.OrdinalIgnoreCase) ? string.Empty : "the ";
            viewModel.Metadata.PageTitle = $"Add a season in {the}{season.Competition.CompetitionName}";

            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = viewModel.Season.Competition.CompetitionName, Url = new Uri(viewModel.Season.Competition.CompetitionRoute, UriKind.Relative) });

            return View("CreateSeason", viewModel);
        }
    }
}