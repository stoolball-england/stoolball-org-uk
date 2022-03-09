using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.Matches.Models
{
    public class DeleteTournamentViewModel : BaseViewModel
    {
        public DeleteTournamentViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
        {
        }
        public Tournament? Tournament { get; set; }
        public MatchListingViewModel Matches { get; set; } = new MatchListingViewModel();
        public MatchingTextConfirmation ConfirmDeleteRequest { get; set; } = new MatchingTextConfirmation();
        public bool Deleted { get; set; }
        public int TotalComments { get; set; }
    }
}