using Stoolball.Dates;
using Stoolball.Matches;
using System.Collections.Generic;

namespace Stoolball.Web.Matches
{
    public class MatchListingViewModel
    {
        public List<MatchListing> Matches { get; internal set; } = new List<MatchListing>();
        public IDateFormatter DateFormatter { get; set; }

        public List<MatchType> MatchTypesToLabel { get; internal set; } = new List<MatchType>();
    }
}