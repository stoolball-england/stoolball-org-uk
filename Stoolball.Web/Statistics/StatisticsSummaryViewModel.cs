using System.Collections.Generic;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;

namespace Stoolball.Web.Statistics
{
    public class StatisticsSummaryViewModel : BaseViewModel
    {
        public StatisticsSummaryViewModel(IPublishedContent contentModel, IUserService userService) : base(contentModel, userService)
        {
        }
        public StatisticsFilter StatisticsFilter { get; set; } = new StatisticsFilter();
        public List<StatisticsResult<PlayerInnings>> PlayerInnings { get; internal set; } = new List<StatisticsResult<PlayerInnings>>();
        public List<StatisticsResult<BowlingFigures>> BowlingFigures { get; internal set; } = new List<StatisticsResult<BowlingFigures>>();
        public List<StatisticsResult<BestTotal>> MostRuns { get; internal set; } = new List<StatisticsResult<BestTotal>>();
    }

    public class StatisticsSummaryViewModel<T> : StatisticsSummaryViewModel
    {
        public StatisticsSummaryViewModel(IPublishedContent contentModel, IUserService userService) : base(contentModel, userService)
        {
        }

        public T Context { get; set; }
        public InningsStatistics InningsStatistics { get; internal set; }
    }
}