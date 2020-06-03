using Stoolball.Clubs;
using Stoolball.Security;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;

namespace Stoolball.Web.Clubs
{
    public class DeleteClubViewModel : BaseViewModel
    {
        public DeleteClubViewModel(IPublishedContent contentModel) : base(contentModel)
        {
        }
        public Club Club { get; set; }
        public MatchingTextConfirmation ConfirmDeleteRequest { get; set; } = new MatchingTextConfirmation();

        public bool Deleted { get; set; }
    }
}