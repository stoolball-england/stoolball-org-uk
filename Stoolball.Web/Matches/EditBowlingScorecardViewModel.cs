using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;

namespace Stoolball.Web.Matches
{
    public class EditBowlingScorecardViewModel : BaseViewModel
    {
        public EditBowlingScorecardViewModel(IPublishedContent contentModel) : base(contentModel)
        {
        }
        public Match Match { get; set; }

        public IDateTimeFormatter DateFormatter { get; set; }
        public int? InningsOrderInMatch { get; set; }
        public MatchInnings CurrentInnings { get; internal set; }
    }
}