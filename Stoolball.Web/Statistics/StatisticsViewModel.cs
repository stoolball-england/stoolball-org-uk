using System;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;

namespace Stoolball.Web.Statistics
{
    public class StatisticsViewModel : BaseViewModel
    {
        public StatisticsViewModel(IPublishedContent contentModel, IUserService userService) : base(contentModel, userService)
        {
        }
        public Guid? BackgroundTaskId { get; set; }
    }
}