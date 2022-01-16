using System.Collections.Generic;
using Stoolball.Matches;
using Stoolball.Statistics;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;

namespace Stoolball.Web.Statistics
{
    public class PlayerBattingViewModel : BasePlayerViewModel
    {
        public PlayerBattingViewModel(IPublishedContent contentModel, IUserService userService) : base(contentModel, userService)
        {
        }
        public List<StatisticsResult<PlayerInnings>> PlayerInnings { get; internal set; } = new List<StatisticsResult<PlayerInnings>>();
        public BattingStatistics BattingStatistics { get; set; }
        public string FilterDescription { get; set; }
    }
}