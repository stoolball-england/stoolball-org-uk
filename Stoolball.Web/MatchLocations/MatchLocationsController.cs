using Stoolball.Umbraco.Data.MatchLocations;
using Stoolball.Web.Routing;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Stoolball.Web.MatchLocations
{
    public class MatchLocationsController : RenderMvcControllerAsync
    {
        private readonly IMatchLocationDataSource _matchLocationDataSource;

        public MatchLocationsController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IMatchLocationDataSource matchLocationDataSource)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _matchLocationDataSource = matchLocationDataSource ?? throw new System.ArgumentNullException(nameof(matchLocationDataSource));
        }

        [HttpGet]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var model = new MatchLocationsViewModel(contentModel.Content)
            {
                MatchLocationQuery = new MatchLocationQuery
                {
                    Query = Request.QueryString["q"]?.Trim()
                }
            };

            if (!string.IsNullOrEmpty(model.MatchLocationQuery.Query))
            {
                model.MatchLocations = await _matchLocationDataSource.ReadMatchLocationListings(model.MatchLocationQuery).ConfigureAwait(false);
            }

            model.Metadata.PageTitle = "Grounds and sports halls";
            if (!string.IsNullOrEmpty(model.MatchLocationQuery.Query))
            {
                model.Metadata.PageTitle += $" matching '{model.MatchLocationQuery.Query}'";
            }

            return CurrentTemplate(model);
        }
    }
}