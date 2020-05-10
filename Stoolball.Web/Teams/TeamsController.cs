using Stoolball.Umbraco.Data.Teams;
using Stoolball.Web.Routing;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Stoolball.Web.Teams
{
    public class TeamsController : RenderMvcControllerAsync
    {
        private readonly ITeamDataSource _teamDataSource;

        public TeamsController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ITeamDataSource teamDataSource)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _teamDataSource = teamDataSource ?? throw new System.ArgumentNullException(nameof(teamDataSource));
        }

        [HttpGet]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var model = new TeamsViewModel(contentModel.Content)
            {
                TeamQuery = new TeamQuery
                {
                    Query = Request.QueryString["q"]?.Trim()
                }
            };

            if (!string.IsNullOrEmpty(model.TeamQuery.Query))
            {
                model.Teams = await _teamDataSource.ReadTeamListings(model.TeamQuery).ConfigureAwait(false);
            }

            model.Metadata.PageTitle = "Stoolball teams";
            if (!string.IsNullOrEmpty(model.TeamQuery.Query))
            {
                model.Metadata.PageTitle += $" matching '{model.TeamQuery.Query}'";
            }

            return CurrentTemplate(model);
        }
    }
}