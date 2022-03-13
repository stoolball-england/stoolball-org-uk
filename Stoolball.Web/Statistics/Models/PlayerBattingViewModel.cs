using System.Collections.Generic;
using Stoolball.Matches;
using Stoolball.Statistics;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.Statistics.Models
{
    public class PlayerBattingViewModel : BasePlayerViewModel
    {
        public PlayerBattingViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
        {
        }
        public List<StatisticsResult<PlayerInnings>> PlayerInnings { get; internal set; } = new List<StatisticsResult<PlayerInnings>>();
        public BattingStatistics BattingStatistics { get; set; } = new BattingStatistics();
        public string? FilterDescription { get; set; }
    }
}