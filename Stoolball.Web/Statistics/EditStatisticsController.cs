using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Data.SqlServer;
using Stoolball.Security;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;
using static Stoolball.Constants;

namespace Stoolball.Web.Statistics
{
    public class EditStatisticsController : RenderMvcControllerAsync
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;

        public EditStatisticsController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IDatabaseConnectionFactory databaseConnectionFactory)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var model = new StatisticsViewModel(contentModel.Content, Services.UserService);
            model.IsAuthorized[AuthorizedAction.EditStatistics] = Members.IsMemberAuthorized(null, new[] { Groups.Administrators }, null);

            model.Metadata.PageTitle = "Update statistics";

            return CurrentTemplate(model);
        }
    }
}