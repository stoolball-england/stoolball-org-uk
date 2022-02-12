using Stoolball.Matches;

namespace Stoolball.Web.Matches.Models
{
    public class MatchListSubscriptionsViewModel
    {
        public string? BaseRoute { get; set; }
        public string FilenameWithoutExtension { get; set; } = "matches";
        public MatchFilter AppliedMatchFilter { get; set; } = new MatchFilter();
        public MatchFilter DefaultMatchFilter { get; set; } = new MatchFilter();
    }
}