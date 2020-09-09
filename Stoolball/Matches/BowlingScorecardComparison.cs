using System.Collections.Generic;

namespace Stoolball.Matches
{
    public class BowlingScorecardComparison
    {
        public List<Over> OversAdded { get; internal set; } = new List<Over>();
        public List<(Over, Over)> OversChanged { get; internal set; } = new List<(Over, Over)>();
        public List<Over> OversRemoved { get; internal set; } = new List<Over>();
        public List<Over> OversUnchanged { get; internal set; } = new List<Over>();
        public List<string> PlayerIdentitiesAdded { get; internal set; } = new List<string>();
        public List<string> PlayerIdentitiesAffected { get; internal set; } = new List<string>();
        public List<string> PlayerIdentitiesRemoved { get; internal set; } = new List<string>();
    }
}