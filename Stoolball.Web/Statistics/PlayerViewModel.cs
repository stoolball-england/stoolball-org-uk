using System.Collections.Generic;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;

namespace Stoolball.Web.Statistics
{
    public class PlayerViewModel : BaseViewModel
    {
        public PlayerViewModel(IPublishedContent contentModel, IUserService userService) : base(contentModel, userService)
        {
        }
        public Player Player { get; set; }
        public List<StatisticsResult<PlayerInnings>> PlayerInnings { get; internal set; } = new List<StatisticsResult<PlayerInnings>>();
        public StatisticsFilter StatisticsFilter { get; set; } = new StatisticsFilter();
        public List<StatisticsResult<BowlingFigures>> BowlingFigures { get; set; }
        public BattingStatistics BattingStatistics { get; set; }
    }
}