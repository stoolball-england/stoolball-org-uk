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
    public class EditSeasonSurfaceController : SurfaceController
    {
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly ISeasonRepository _seasonRepository;
        private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;

        public EditSeasonSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, ISeasonDataSource seasonDataSource,
            ISeasonRepository seasonRepository, IAuthorizationPolicy<Competition> authorizationPolicy)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(TinyMCE = true, Forms = true)]
        public async Task<ActionResult> UpdateSeason([Bind(Prefix = "Season", Include = "EnableTournaments,PlayersPerTeam,DefaultOverSets,EnableLastPlayerBatsOn,EnableBonusOrPenaltyRuns")] Season season)
        {
            if (season is null)
            {
                throw new ArgumentNullException(nameof(season));
            }

            var beforeUpdate = await _seasonDataSource.ReadSeasonByRoute(Request.RawUrl).ConfigureAwait(false);

            season.DefaultOverSets.RemoveAll(x => !x.Overs.HasValue);

            // get this from the unvalidated form instead of via modelbinding so that HTML can be allowed
            season.SeasonId = beforeUpdate.SeasonId;
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

            if (!season.MatchTypes.Any())
            {
                ModelState.AddModelError("Season.MatchTypes", $"Please select at least one type of match");
            }

            var isAuthorized = _authorizationPolicy.IsAuthorized(beforeUpdate.Competition);

            if (isAuthorized[AuthorizedAction.EditCompetition] && ModelState.IsValid)
            {
                // Update the season
                var currentMember = Members.GetCurrentMember();
                await _seasonRepository.UpdateSeason(season, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                // Redirect to the season actions page that led here
                return Redirect(beforeUpdate.SeasonRoute + "/edit");
            }

            var viewModel = new SeasonViewModel(CurrentPage, Services.UserService)
            {
                Season = beforeUpdate,
            };
            viewModel.IsAuthorized = isAuthorized;
            viewModel.Metadata.PageTitle = $"Edit {beforeUpdate.SeasonFullNameAndPlayerType()}";

            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = viewModel.Season.Competition.CompetitionName, Url = new Uri(viewModel.Season.Competition.CompetitionRoute, UriKind.Relative) });
            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = viewModel.Season.SeasonName(), Url = new Uri(viewModel.Season.SeasonRoute, UriKind.Relative) });

            return View("EditSeason", viewModel);
        }
    }
}