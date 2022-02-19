using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Competitions;
using Stoolball.Listings;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Competitions
{
    public class CompetitionsController : RenderController, IRenderControllerAsync
    {
        private readonly ICompetitionDataSource _competitionDataSource;
        private readonly IListingsModelBuilder<Competition, CompetitionFilter, CompetitionsViewModel> _listingsModelBuilder;

        public CompetitionsController(ILogger<CompetitionsController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            ICompetitionDataSource competitionDataSource,
            IListingsModelBuilder<Competition, CompetitionFilter, CompetitionsViewModel> listingsModelBuilder)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _competitionDataSource = competitionDataSource ?? throw new ArgumentNullException(nameof(competitionDataSource));
            _listingsModelBuilder = listingsModelBuilder ?? throw new ArgumentNullException(nameof(listingsModelBuilder));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var model = await _listingsModelBuilder.BuildModel(
                () => new CompetitionsViewModel(CurrentPage),
                _competitionDataSource.ReadTotalCompetitions,
                _competitionDataSource.ReadCompetitions,
                Constants.Pages.Competitions,
                new Uri(Request.GetEncodedUrl()),
                Request.QueryString.Value).ConfigureAwait(false);

            return CurrentTemplate(model);
        }
    }
}