using Stoolball.Matches;
using Stoolball.Web.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.Matches.Models
{
    public class TournamentViewModel : BaseViewModel
    {
        public TournamentViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
        {
        }
        public Tournament? Tournament { get; set; }
        public MatchListingViewModel Matches { get; set; } = new MatchListingViewModel();
    }
}