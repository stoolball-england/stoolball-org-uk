using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Competitions;
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
            var model = new CompetitionsViewModel(contentModel.Content, Services?.UserService)
            {
                CompetitionFilter = new CompetitionFilter
                {
                    Query = Request.QueryString["q"]?.Trim()
                }
            };

            model.CompetitionFilter.Paging.PageNumber = pageNumber > 0 ? pageNumber : 1;
            model.CompetitionFilter.Paging.PageSize = Constants.Defaults.PageSize;
            model.CompetitionFilter.Paging.PageUrl = Request.Url;
            model.CompetitionFilter.Paging.Total = await _competitionDataSource.ReadTotalCompetitions(model.CompetitionFilter).ConfigureAwait(false);
            model.Competitions = await _competitionDataSource.ReadCompetitions(model.CompetitionFilter).ConfigureAwait(false);

            model.Metadata.PageTitle = Constants.Pages.Competitions;
            if (!string.IsNullOrEmpty(model.CompetitionFilter.Query))
            {
                model.Metadata.PageTitle += $" matching '{model.CompetitionFilter.Query}'";
            }

            return CurrentTemplate(model);
        }
    }
}