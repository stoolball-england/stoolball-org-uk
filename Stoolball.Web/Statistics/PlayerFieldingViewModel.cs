using System.Collections.Generic;
using Stoolball.Matches;
using Stoolball.Statistics;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;

namespace Stoolball.Web.Statistics
{
    public class PlayerFieldingViewModel : BasePlayerViewModel
    {
        public PlayerFieldingViewModel(IPublishedContent contentModel, IUserService userService) : base(contentModel, userService)
        {
        }
        public FieldingStatistics FieldingStatistics { get; set; }
        public List<StatisticsResult<PlayerInnings>> Catches { get; internal set; } = new List<StatisticsResult<PlayerInnings>>();
        public List<StatisticsResult<PlayerInnings>> RunOuts { get; internal set; } = new List<StatisticsResult<PlayerInnings>>();
    }
}