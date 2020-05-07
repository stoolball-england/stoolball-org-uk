using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;

namespace Stoolball.Web.Matches
{
    public class MatchViewModel : BaseViewModel
    {
        public MatchViewModel(IPublishedContent contentModel) : base(contentModel)
        {
        }
        public Match Match { get; set; }
        public IDateTimeFormatter DateTimeFormatter { get; set; }
    }
}