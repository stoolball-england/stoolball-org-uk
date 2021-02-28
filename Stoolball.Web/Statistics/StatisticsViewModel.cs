using System.Collections.Generic;
using Stoolball.Matches;
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
        public StatisticsFilter StatisticsFilter { get; set; } = new StatisticsFilter();
        public List<StatisticsResult<PlayerInnings>> PlayerInnings { get; internal set; } = new List<StatisticsResult<PlayerInnings>>();
    }

    public class StatisticsViewModel<T> : StatisticsViewModel
    {
        public StatisticsViewModel(IPublishedContent contentModel, IUserService userService) : base(contentModel, userService)
        {
        }

        public T Context { get; set; }
    }
}