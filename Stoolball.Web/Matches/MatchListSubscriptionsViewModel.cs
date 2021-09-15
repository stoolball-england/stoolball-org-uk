using Stoolball.Matches;

namespace Stoolball.Web.Matches
{
    public class MatchListSubscriptionsViewModel
    {
        public string BaseRoute { get; set; }
        public string FilenameWithoutExtension { get; set; } = "matches";
        public MatchFilter MatchFilter { get; set; }
    }
}