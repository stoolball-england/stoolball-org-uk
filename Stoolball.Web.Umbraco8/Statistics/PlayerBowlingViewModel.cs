using System.Collections.Generic;
using Stoolball.Statistics;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;

namespace Stoolball.Web.Statistics
{
    public class PlayerBowlingViewModel : BasePlayerViewModel
    {
        public PlayerBowlingViewModel(IPublishedContent contentModel, IUserService userService) : base(contentModel, userService)
        {
        }
        public List<StatisticsResult<BowlingFigures>> BowlingFigures { get; internal set; } = new List<StatisticsResult<BowlingFigures>>();
        public BowlingStatistics BowlingStatistics { get; set; }
        public string FilterDescription { get; set; }
    }
}