using System.Collections.Generic;
using Stoolball.Statistics;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;

namespace Stoolball.Web.Statistics
{
    public class IndividualScoresViewModel : BaseViewModel
    {
        public IndividualScoresViewModel(IPublishedContent contentModel, IUserService userService) : base(contentModel, userService)
        {
        }
        public List<PlayerInningsResult> Results { get; internal set; } = new List<PlayerInningsResult>();
        public int TotalResults { get; set; }
        public StatisticsFilter StatisticsFilter { get; set; }
    }
}