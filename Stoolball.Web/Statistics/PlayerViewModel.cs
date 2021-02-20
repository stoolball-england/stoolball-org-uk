using Stoolball.Statistics;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;

namespace Stoolball.Web.Statistics
{
    public class PlayerViewModel : BaseViewModel
    {
        public PlayerViewModel(IPublishedContent contentModel, IUserService userService) : base(contentModel, userService)
        {
        }
        public Player Player { get; set; }
    }
}