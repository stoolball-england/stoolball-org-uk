using System.Collections.Generic;
using Stoolball.Matches;
using Stoolball.Statistics;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.Statistics.Models
{
    public class PlayerFieldingViewModel : BasePlayerViewModel
    {
        public PlayerFieldingViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
        {
        }
        public FieldingStatistics FieldingStatistics { get; set; } = new FieldingStatistics();
        public List<StatisticsResult<PlayerInnings>> Catches { get; internal set; } = new List<StatisticsResult<PlayerInnings>>();
        public List<StatisticsResult<PlayerInnings>> RunOuts { get; internal set; } = new List<StatisticsResult<PlayerInnings>>();
    }
}