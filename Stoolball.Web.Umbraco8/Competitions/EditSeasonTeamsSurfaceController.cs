using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Competitions;
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
    public class EditSeasonTeamsSurfaceController : SurfaceController
    {
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly ISeasonRepository _seasonRepository;
        private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;

        public EditSeasonTeamsSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
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
        [ContentSecurityPolicy(Forms = true)]
        public async Task<ActionResult> UpdateSeason([Bind(Prefix = "Season", Include = "Teams")] Season season)
        {
            if (season is null)
            {
                season = new Season(); // if there are no teams, season is null
            }

            var beforeUpdate = await _seasonDataSource.ReadSeasonByRoute(Request.RawUrl).ConfigureAwait(false);
            season.SeasonId = beforeUpdate.SeasonId;

            ReplaceDateFormatErrorMessages("Date withdrew");

            // We're not interested in validating the details of the selected teams
            foreach (var key in ModelState.Keys.Where(x => x.StartsWith("Season.Teams", StringComparison.OrdinalIgnoreCase)))
            {
                ModelState[key].Errors.Clear();
            }

            var isAuthorized = _authorizationPolicy.IsAuthorized(beforeUpdate.Competition);

            if (isAuthorized[AuthorizedAction.EditCompetition] && ModelState.IsValid)
            {
                // Update the season
                var currentMember = Members.GetCurrentMember();
                await _seasonRepository.UpdateTeams(season, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                // Redirect to the season actions page that led here
                return Redirect(beforeUpdate.SeasonRoute + "/edit");
            }

            var viewModel = new SeasonViewModel(CurrentPage, Services.UserService)
            {
                Season = season,
            };
            viewModel.IsAuthorized = isAuthorized;
            season.Competition = beforeUpdate.Competition;
            season.FromYear = beforeUpdate.FromYear;
            season.UntilYear = beforeUpdate.UntilYear;
            season.SeasonRoute = beforeUpdate.SeasonRoute;

            viewModel.Metadata.PageTitle = $"Teams in the {beforeUpdate.SeasonFullNameAndPlayerType()}";

            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = viewModel.Season.Competition.CompetitionName, Url = new Uri(viewModel.Season.Competition.CompetitionRoute, UriKind.Relative) });
            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = viewModel.Season.SeasonName(), Url = new Uri(viewModel.Season.SeasonRoute, UriKind.Relative) });

            return View("EditSeasonTeams", viewModel);
        }

        /// <summary>
        /// ModelState date format error messages for date fields only say its invalid, without saying YYYY-MM-DD is the valid format. Update the message to be more helpful.
        /// </summary>
        /// <remarks>
        /// Only browsers which do not support HTML <input type="date" /> should ever see this message, which mainly means Safari.
        /// Using a [RegularExpression] validator on the property in order to set the ErrorMessage property does not work, because it does not allow a valid value to pass from a browser that supports <input type="date" />.
        /// </remarks>
        /// <param name="fieldDisplayName">The field name used in the default error message, which is xxx when using System.ComponentModel.DataAnnotations [Display(Name = "xxx")]</param>
        private void ReplaceDateFormatErrorMessages(string fieldDisplayName)
        {
            var fieldsWithDateFormatErrors = ModelState.Keys.Where(x => ModelState[x].Errors.Count > 0 && ModelState[x].Errors.Any(e => e.ErrorMessage.EndsWith($"is not valid for {fieldDisplayName}.", StringComparison.OrdinalIgnoreCase)));
            foreach (var fieldName in fieldsWithDateFormatErrors)
            {
                var dateFormatError = ModelState[fieldName].Errors.Single(e => e.ErrorMessage.EndsWith($"is not valid for {fieldDisplayName}.", StringComparison.OrdinalIgnoreCase));
                ModelState[fieldName].Errors.Remove(dateFormatError);
                ModelState.AddModelError(fieldName, "Enter a date in YYYY-MM-DD format.");
            }
        }
    }
}