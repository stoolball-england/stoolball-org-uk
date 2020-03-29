using Stoolball.Clubs;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;

namespace Stoolball.Web.Clubs
{
    public class ClubViewModel : BaseViewModel
    {
        public ClubViewModel(IPublishedContent contentModel) : base(contentModel)
        {
        }
        public Club Club { get; set; }
    }
}