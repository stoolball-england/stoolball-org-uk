using System.Collections.Generic;
using Stoolball.Statistics;
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
        public List<PlayerInningsResult> PlayerInnings { get; internal set; } = new List<PlayerInningsResult>();
        public StatisticsFilter StatisticsFilter { get; set; } = new StatisticsFilter();
    }
}