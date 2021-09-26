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
        public string FilterDescription { get; set; }
        public StatisticsFilter DefaultFilter { get; set; } = new StatisticsFilter();
        public StatisticsFilter AppliedFilter { get; set; } = new StatisticsFilter();
        public List<StatisticsResult<PlayerInnings>> PlayerInnings { get; internal set; } = new List<StatisticsResult<PlayerInnings>>();
        public List<StatisticsResult<BowlingFigures>> BowlingFigures { get; internal set; } = new List<StatisticsResult<BowlingFigures>>();
        public List<StatisticsResult<BestStatistic>> MostRuns { get; internal set; } = new List<StatisticsResult<BestStatistic>>();
        public List<StatisticsResult<BestStatistic>> MostWickets { get; internal set; } = new List<StatisticsResult<BestStatistic>>();
        public List<StatisticsResult<BestStatistic>> MostCatches { get; internal set; } = new List<StatisticsResult<BestStatistic>>();
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