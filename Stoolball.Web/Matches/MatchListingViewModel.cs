using System.Collections.Generic;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;

namespace Stoolball.Web.Matches
{
    public class MatchListingViewModel : BaseViewModel
    {
        public MatchListingViewModel(IPublishedContent contentModel, IUserService userService) : base(contentModel, userService) { }

        public List<MatchListing> Matches { get; internal set; } = new List<MatchListing>();
        public IDateTimeFormatter DateTimeFormatter { get; set; }
        public MatchFilter DefaultMatchFilter { get; set; } = new MatchFilter();
        public MatchFilter AppliedMatchFilter { get; set; } = new MatchFilter();
        public string FilterDescription { get; set; }
        public List<MatchType> MatchTypesToLabel { get; internal set; } = new List<MatchType>();
        public bool ShowMatchDate { get; set; } = true;
        public bool HighlightNextMatch { get; set; } = true;
    }
}