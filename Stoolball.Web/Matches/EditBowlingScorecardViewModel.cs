using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;

namespace Stoolball.Web.Matches
{
    public class EditBowlingScorecardViewModel : BaseViewModel
    {
        public EditBowlingScorecardViewModel(IPublishedContent contentModel, IUserService userService) : base(contentModel, userService)
        {
        }
        public Match Match { get; set; }

        public IDateTimeFormatter DateFormatter { get; set; }
        public int? InningsOrderInMatch { get; set; }
        public MatchInnings CurrentInnings { get; internal set; }
    }
}