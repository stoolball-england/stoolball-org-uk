using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Web.Common.Controllers;
using static Stoolball.Constants;

namespace Stoolball.Web.Statistics.Admin
{
    public class EditStatisticsApiController : UmbracoApiController
    {
        private readonly IMemberManager _memberManager;
        private readonly IBackgroundTaskTracker _taskTracker;

        public EditStatisticsApiController(IMemberManager memberManager, IBackgroundTaskTracker taskTracker) : base()
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _taskTracker = taskTracker ?? throw new ArgumentNullException(nameof(taskTracker));
        }

        [HttpGet]
        [Route("api/statistics/progress")]
        public async Task<IActionResult> Progress([FromQuery] Guid taskId)
        {
            if (await _memberManager.IsMemberAuthorizedAsync(null, new[] { Groups.Administrators }, null))
            {
                return new JsonResult(new ProgressResult { percent = _taskTracker.ProgressPercentage(taskId), errors = _taskTracker.TotalErrors(taskId) });
            }
            return Unauthorized();
        }
    }
}