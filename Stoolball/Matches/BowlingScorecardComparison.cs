using System.Collections.Generic;

namespace Stoolball.Matches
{
    public class BowlingScorecardComparison
    {
        public List<Over> OversAdded { get; internal set; } = new List<Over>();
        public List<(Over, Over)> OversChanged { get; internal set; } = new List<(Over, Over)>();
        public List<Over> OversRemoved { get; internal set; } = new List<Over>();
        public List<Over> OversUnchanged { get; internal set; } = new List<Over>();

        /// <summary>
        /// Player identities which were not featured in the 'before' scorecard.
        /// </summary>
        public List<string> PlayerIdentitiesAdded { get; internal set; } = new List<string>();

        /// <summary>
        /// Player identities which were added, removed, or had their bowling figures changed in the 'after' scorecard.
        /// </summary>
        public List<string> PlayerIdentitiesAffected { get; internal set; } = new List<string>();

        /// <summary>
        /// Player identities which were not featured in the 'after' scorecard.
        /// </summary>
        public List<string> PlayerIdentitiesRemoved { get; internal set; } = new List<string>();
    }
}