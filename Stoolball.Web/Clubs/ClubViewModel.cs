using Stoolball.Clubs;
using Stoolball.Web.Matches;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;

namespace Stoolball.Web.Clubs
{
    public class ClubViewModel : BaseViewModel
    {
        public ClubViewModel(IPublishedContent contentModel, IUserService userService) : base(contentModel, userService)
        {
        }
        public Club Club { get; set; }
        public MatchListingViewModel Matches { get; set; }
    }
}