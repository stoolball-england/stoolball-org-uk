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
    }
}