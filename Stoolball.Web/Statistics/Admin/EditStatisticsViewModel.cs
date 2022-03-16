using System;
using Stoolball.Web.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.Statistics.Admin
{
    public class EditStatisticsViewModel : BaseViewModel
    {
        public EditStatisticsViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
        {
        }
        public Guid? BackgroundTaskId { get; set; }
    }
}