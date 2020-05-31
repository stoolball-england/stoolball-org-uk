using Stoolball.Audit;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Stoolball.Teams
{
    public class PlayerIdentity : IAuditable
    {
        public Guid? PlayerIdentityId { get; set; }
        public Guid? PlayerId { get; set; }

        public Team Team { get; set; }
        public string PlayerIdentityName { get; set; }

        /// <summary>
        /// Gets the version of the player's name used to match them against partial names
        /// </summary>
        /// <returns></returns>
        public string ComparableName()
        {
            return Regex.Replace(PlayerIdentityName, "[^A-Z0-9]", string.Empty, RegexOptions.IgnoreCase).ToUpperInvariant();
        }

        public DateTime? FirstPlayed { get; set; }

        public DateTime? LastPlayed { get; set; }

        public int? TotalMatches { get; set; }

        public int? MissedMatches { get; set; }

        public int? Probability { get; set; }

        public PlayerRole PlayerRole { get; set; }

        public string PlayerIdentityRoute { get; set; }
        public List<AuditRecord> History { get; internal set; } = new List<AuditRecord>();

        public Uri EntityUri
        {
            get { return new Uri($"https://www.stoolball.org.uk/id/player/{PlayerIdentityId}"); }
        }
    }
}
