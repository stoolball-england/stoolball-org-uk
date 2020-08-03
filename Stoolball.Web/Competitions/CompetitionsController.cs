using Stoolball.Umbraco.Data.Competitions;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using System.Threading.Tasks;
using System.Web.Mvc;
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

        public CompetitionsController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ICompetitionDataSource competitionDataSource)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _competitionDataSource = competitionDataSource ?? throw new System.ArgumentNullException(nameof(competitionDataSource));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            _ = int.TryParse(Request.QueryString["page"], out var pageNumber);
            var model = new CompetitionsViewModel(contentModel.Content)
            {
                CompetitionQuery = new CompetitionQuery
                {
                    Query = Request.QueryString["q"]?.Trim(),
                    PageNumber = pageNumber > 0 ? pageNumber : 1
                }
            };

            model.TotalCompetitions = await _competitionDataSource.ReadTotalCompetitions(model.CompetitionQuery).ConfigureAwait(false);
            model.Competitions = await _competitionDataSource.ReadCompetitions(model.CompetitionQuery).ConfigureAwait(false);

            model.Metadata.PageTitle = "Stoolball competitions";
            if (!string.IsNullOrEmpty(model.CompetitionQuery.Query))
            {
                model.Metadata.PageTitle += $" matching '{model.CompetitionQuery.Query}'";
            }

            return CurrentTemplate(model);
        }
    }
}