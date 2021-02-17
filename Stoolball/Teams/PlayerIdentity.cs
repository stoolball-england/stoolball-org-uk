using System;
using System.Text.RegularExpressions;

namespace Stoolball.Teams
{
    public class PlayerIdentity
    {
        public Guid? PlayerIdentityId { get; set; }

        public Team Team { get; set; }
        public string PlayerIdentityName { get; set; }

        /// <summary>
        /// Gets the version of the player's name used to match them against partial names
        /// </summary>
        /// <returns></returns>
        public string ComparableName()
        {
            return string.IsNullOrEmpty(PlayerIdentityName) ? PlayerIdentityName : Regex.Replace(PlayerIdentityName, "[^A-Z0-9]", string.Empty, RegexOptions.IgnoreCase).ToUpperInvariant();
        }

        public Player Player { get; set; }

        public DateTimeOffset? FirstPlayed { get; set; }

        public DateTimeOffset? LastPlayed { get; set; }

        public int? TotalMatches { get; set; }

        public int? MissedMatches { get; set; }

        public int? Probability { get; set; }
    }
}
