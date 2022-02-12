using Stoolball.Clubs;
using Stoolball.Matches;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.Clubs
{
    public class ClubViewModel : BaseViewModel
    {
        public ClubViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
        {
        }
        public Club? Club { get; set; }
        public MatchListingViewModel Matches { get; set; } = new MatchListingViewModel();
        public MatchFilter DefaultMatchFilter { get; set; } = new MatchFilter();
        public MatchFilter AppliedMatchFilter { get; set; } = new MatchFilter();
        public string? FilterDescription { get; set; }
    }
}