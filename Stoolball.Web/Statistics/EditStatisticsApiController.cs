using System;
using System.Net;
using System.Web.Http;
using Stoolball.Web.WebApi;
using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Mapping;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.WebApi;
using static Stoolball.Constants;

namespace Stoolball.Web.Statistics
{
    public class EditStatisticsApiController : UmbracoApiController
    {
        private readonly IBackgroundTaskTracker _taskTracker;

        public EditStatisticsApiController(IGlobalSettings globalSettings, IUmbracoContextAccessor umbracoContextAccessor, ISqlContext sqlContext, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IRuntimeState runtimeState, UmbracoHelper umbracoHelper, UmbracoMapper umbracoMapper,
            IBackgroundTaskTracker taskTracker) :
            base(globalSettings, umbracoContextAccessor, sqlContext, serviceContext, appCaches, profilingLogger, runtimeState, umbracoHelper, umbracoMapper)
        {
            _taskTracker = taskTracker ?? throw new ArgumentNullException(nameof(taskTracker));
        }

        [HttpGet]
        [Route("api/statistics/progress")]
        public ProgressResult Progress([FromUri] Guid taskId)
        {
            if (Members.IsMemberAuthorized(null, new[] { Groups.Administrators }, null))
            {
                return new ProgressResult { percent = _taskTracker.ProgressPercentage(taskId), errors = _taskTracker.TotalErrors(taskId) };
            }
            throw new HttpResponseException(HttpStatusCode.Unauthorized);
        }
    }
}