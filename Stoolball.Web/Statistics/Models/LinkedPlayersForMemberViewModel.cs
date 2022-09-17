using Stoolball.Statistics;
using Stoolball.Web.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.Statistics.Models
{
    public class LinkedPlayersForMemberViewModel : BaseViewModel
    {
        public LinkedPlayersForMemberViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
        {
        }

        public Player? Player { get; set; }

        public string PreferredNextRoute { get; set; } = Constants.Pages.AccountUrl;
    }
}
