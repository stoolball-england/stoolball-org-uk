using Stoolball.Competitions;
using Stoolball.Teams;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;

namespace Stoolball.Web.Matches
{
    public class CreateMatchViewModel : BaseViewModel
    {
        public CreateMatchViewModel(IPublishedContent contentModel) : base(contentModel)
        {
        }
        public Team Team { get; set; }
        public Season Season { get; set; }
    }
}