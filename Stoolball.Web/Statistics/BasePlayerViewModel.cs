using Stoolball.Statistics;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;

namespace Stoolball.Web.Statistics
{
    public abstract class BasePlayerViewModel : BaseViewModel
    {
        protected BasePlayerViewModel(IPublishedContent contentModel, IUserService userService) : base(contentModel, userService)
        {
        }
        public Player Player { get; set; }
        public StatisticsFilter StatisticsFilter { get; set; } = new StatisticsFilter();
    }
}