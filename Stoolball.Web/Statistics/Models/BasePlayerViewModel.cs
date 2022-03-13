using Stoolball.Statistics;
using Stoolball.Web.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.Statistics
{
    public abstract class BasePlayerViewModel : BaseViewModel
    {
        protected BasePlayerViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
        {
        }
        public Player? Player { get; set; }
        public StatisticsFilter DefaultFilter { get; set; } = new StatisticsFilter();
        public StatisticsFilter AppliedFilter { get; set; } = new StatisticsFilter();
    }
}