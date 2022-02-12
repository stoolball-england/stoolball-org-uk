using System.Collections.Generic;
using Stoolball.Matches;
using Stoolball.Web.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.Matches.Models
{
    public class MatchListingViewModel : BaseViewModel
    {
        public MatchListingViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService) { }

        public List<MatchListing> Matches { get; internal set; } = new List<MatchListing>();
        public MatchFilter DefaultMatchFilter { get; set; } = new MatchFilter();
        public MatchFilter AppliedMatchFilter { get; set; } = new MatchFilter();
        public string? FilterDescription { get; set; }
        public List<MatchType> MatchTypesToLabel { get; internal set; } = new List<MatchType>();
        public bool ShowMatchDate { get; set; } = true;
        public bool HighlightNextMatch { get; set; } = true;
    }
}