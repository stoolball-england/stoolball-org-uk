using System.Collections.Generic;
using Stoolball.Statistics;
using Stoolball.Web.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.Statistics.Models
{
    public class StatisticsViewModel<T> : BaseViewModel
    {
        public StatisticsViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
        {
        }
        public List<StatisticsResult<T>> Results { get; internal set; } = new List<StatisticsResult<T>>();
        public StatisticsFilter DefaultFilter { get; set; } = new StatisticsFilter();
        public StatisticsFilter AppliedFilter { get; set; } = new StatisticsFilter();
        public string? FilterDescription { get; set; }
        public bool ShowLinkOnly { get; set; }
        public bool ShowCaption { get; set; } = true;
        public bool ShowPlayerColumn { get; set; } = true;
        public bool LinkPlayer { get; set; } = true;
        public bool ShowTeamsColumn { get; set; } = true;
    }
}