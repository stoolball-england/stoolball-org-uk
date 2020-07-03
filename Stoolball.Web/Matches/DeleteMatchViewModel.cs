using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;

namespace Stoolball.Web.Matches
{
    public class DeleteMatchViewModel : BaseViewModel
    {
        public DeleteMatchViewModel(IPublishedContent contentModel) : base(contentModel)
        {
        }
        public Match Match { get; set; }
        public IDateTimeFormatter DateTimeFormatter { get; set; }
        public MatchingTextConfirmation ConfirmDeleteRequest { get; set; } = new MatchingTextConfirmation();
        public bool Deleted { get; set; }
        public int TotalComments { get; set; }
    }
}