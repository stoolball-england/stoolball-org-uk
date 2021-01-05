using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Competitions;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Stoolball.Web.Competitions
{
    public class CreateSeasonController : RenderMvcControllerAsync
    {
        private readonly ICompetitionDataSource _competitionDataSource;
        private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;

        public CreateSeasonController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ICompetitionDataSource competitionDataSource,
           IAuthorizationPolicy<Competition> authorizationPolicy)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _competitionDataSource = competitionDataSource ?? throw new ArgumentNullException(nameof(competitionDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
        }

        [HttpGet]
        [ContentSecurityPolicy(TinyMCE = true, Forms = true)]
        public override async Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var competition = await _competitionDataSource.ReadCompetitionByRoute(Request.RawUrl).ConfigureAwait(false);

            if (competition == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                var model = new SeasonViewModel(contentModel.Content, Services?.UserService)
                {
                    Season = competition.Seasons.LastOrDefault() ?? new Season { PlayersPerTeam = 11 }
                };
                var summerSeason = model.Season.FromYear == model.Season.UntilYear;
                model.Season.Competition = competition;
                model.Season.FromYear = model.Season.FromYear == default ? DateTime.Today.Year : model.Season.FromYear + 1;
                model.Season.UntilYear = summerSeason ? 0 : 1;

                model.IsAuthorized = _authorizationPolicy.IsAuthorized(model.Season.Competition);

                var the = model.Season.Competition.CompetitionName.StartsWith("THE ", StringComparison.OrdinalIgnoreCase) ? string.Empty : "the ";
                model.Metadata.PageTitle = $"Add a season in {the}{model.Season.Competition.CompetitionName}";

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Season.Competition.CompetitionName, Url = new Uri(model.Season.Competition.CompetitionRoute, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}