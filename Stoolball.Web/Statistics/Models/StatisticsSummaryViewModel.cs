using System.Collections.Generic;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Web.Filtering;
using Stoolball.Web.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.Statistics.Models
{
    public class StatisticsSummaryViewModel : BaseViewModel
    {
        public StatisticsSummaryViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
        {
        }
        public FilterViewModel FilterViewModel { get; set; } = new();
        public StatisticsFilter DefaultFilter { get; set; } = new();
        public StatisticsFilter AppliedFilter { get; set; } = new();
        public List<StatisticsResult<PlayerInnings>> PlayerInnings { get; internal set; } = new();
        public List<StatisticsResult<BowlingFigures>> BowlingFigures { get; internal set; } = new();
        public List<StatisticsResult<BestStatistic>> MostRuns { get; internal set; } = new();
        public List<StatisticsResult<BestStatistic>> MostWickets { get; internal set; } = new();
        public List<StatisticsResult<BestStatistic>> MostCatches { get; internal set; } = new();
    }

    public class StatisticsSummaryViewModel<T> : StatisticsSummaryViewModel
    {
        public StatisticsSummaryViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
        {
        }

        public T? Context { get; set; }
        public InningsStatistics InningsStatistics { get; internal set; } = new InningsStatistics();
    }
}