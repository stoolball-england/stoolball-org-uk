using Stoolball.Competitions;
using Stoolball.Umbraco.Data.Competitions;
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
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Web.Competitions
{
    public class EditSeasonPointsSurfaceController : SurfaceController
    {
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly ISeasonRepository _seasonRepository;

        public EditSeasonPointsSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, ISeasonDataSource seasonDataSource,
            ISeasonRepository seasonRepository)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _seasonRepository = seasonRepository ?? throw new System.ArgumentNullException(nameof(seasonRepository));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<ActionResult> UpdateSeason([Bind(Prefix = "Season", Include = "PointsRules")] Season season)
        {
            if (season is null)
            {
                throw new ArgumentNullException(nameof(season));
            }

            var beforeUpdate = await _seasonDataSource.ReadSeasonByRoute(Request.RawUrl).ConfigureAwait(false);
            season.SeasonId = beforeUpdate.SeasonId;

            var isAuthorized = Members.IsMemberAuthorized(null, new[] { Groups.Administrators, beforeUpdate.Competition.MemberGroupName }, null);

            if (isAuthorized && ModelState.IsValid)
            {
                // Update the season
                var currentMember = Members.GetCurrentMember();
                await _seasonRepository.UpdateSeasonPoints(season, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                // Redirect to the season actions page that led here
                return Redirect(beforeUpdate.SeasonRoute + "/edit");
            }

            var viewModel = new SeasonViewModel(CurrentPage)
            {
                Season = season,
                IsAuthorized = isAuthorized
            };
            season.Competition = beforeUpdate.Competition;
            season.FromYear = beforeUpdate.FromYear;
            season.UntilYear = beforeUpdate.UntilYear;
            season.SeasonRoute = beforeUpdate.SeasonRoute;

            viewModel.Metadata.PageTitle = $"Edit points for {beforeUpdate.SeasonFullNameAndPlayerType()}";
            return View("EditSeasonResults", viewModel);
        }
    }
}