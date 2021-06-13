using System.Collections.Generic;
using Stoolball.Statistics;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;

namespace Stoolball.Web.Statistics
{
    public class StatisticsViewModel<T> : BaseViewModel
    {
        public StatisticsViewModel(IPublishedContent contentModel, IUserService userService) : base(contentModel, userService)
        {
        }
        public List<StatisticsResult<T>> Results { get; internal set; } = new List<StatisticsResult<T>>();
        public StatisticsFilter StatisticsFilter { get; set; } = new StatisticsFilter();
        public bool ShowLinkOnly { get; set; }
        public bool ShowCaption { get; set; } = true;
        public bool ShowPlayerColumn { get; set; } = true;
        public bool ShowTeamsColumn { get; set; } = true;
    }
}