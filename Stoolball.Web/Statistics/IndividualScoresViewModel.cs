using System.Collections.Generic;
using Stoolball.Matches;
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
        public List<StatisticsResult<PlayerInnings>> Results { get; internal set; } = new List<StatisticsResult<PlayerInnings>>();
        public int TotalResults { get; set; }
        public StatisticsFilter StatisticsFilter { get; set; } = new StatisticsFilter();

        public bool ShowCaption { get; set; } = true;
        public bool ShowPlayerColumn { get; set; } = true;
    }
}