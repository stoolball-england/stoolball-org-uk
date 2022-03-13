using System.Collections.Generic;
using Stoolball.Statistics;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.Statistics.Models
{
    public class PlayerBowlingViewModel : BasePlayerViewModel
    {
        public PlayerBowlingViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
        {
        }
        public List<StatisticsResult<BowlingFigures>> BowlingFigures { get; internal set; } = new List<StatisticsResult<BowlingFigures>>();
        public BowlingStatistics BowlingStatistics { get; set; } = new BowlingStatistics();
        public string? FilterDescription { get; set; }
    }
}