using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Competitions;
using Stoolball.Listings;
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
    public class CompetitionsController : RenderMvcControllerAsync
    {
        private readonly ICompetitionDataSource _competitionDataSource;
        private readonly IListingsModelBuilder<Competition, CompetitionFilter, CompetitionsViewModel> _listingsModelBuilder;

        public CompetitionsController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ICompetitionDataSource competitionDataSource,
           IListingsModelBuilder<Competition, CompetitionFilter, CompetitionsViewModel> listingsModelBuilder)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _competitionDataSource = competitionDataSource ?? throw new System.ArgumentNullException(nameof(competitionDataSource));
            _listingsModelBuilder = listingsModelBuilder ?? throw new System.ArgumentNullException(nameof(listingsModelBuilder));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var model = await _listingsModelBuilder.BuildModel(
                () => new CompetitionsViewModel(contentModel.Content, Services?.UserService),
                _competitionDataSource.ReadTotalCompetitions,
                _competitionDataSource.ReadCompetitions,
                Constants.Pages.Competitions,
                Request.Url,
                Request.QueryString).ConfigureAwait(false);

            return CurrentTemplate(model);
        }
    }
}