using Stoolball.Clubs;
using Stoolball.Security;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;

namespace Stoolball.Web.Clubs
{
    public class DeleteClubViewModel : BaseViewModel
    {
        public DeleteClubViewModel(IPublishedContent contentModel, IUserService userService) : base(contentModel, userService)
        {
        }
        public Club Club { get; set; }
        public MatchingTextConfirmation ConfirmDeleteRequest { get; set; } = new MatchingTextConfirmation();

        public bool Deleted { get; set; }
    }
}