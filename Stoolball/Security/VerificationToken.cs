using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Stoolball.Security
{
    /// <summary>
    /// A verification token in the format {id}-{guid}
    /// </summary>
    public class VerificationToken : IVerificationToken
    {
        /// <summary>
        /// Generates a new token based on the supplied id
        /// </summary>
        public (string token, DateTime expires) TokenFor(int id)
        {
            return ($"{id}-{Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)}", DateTime.UtcNow.AddDays(1));
        }

        /// <summary>
        /// Extracts the id part of a token
        /// </summary>
        /// <exception cref="FormatException"></exception>
        public int ExtractId(string token)
        {
            if (string.IsNullOrEmpty(token)) { throw new FormatException(); }

            var match = Regex.Match(token, "^(?<Id>[0-9]+)-[0-9a-f]{32}$");
            if (!match.Success) { throw new FormatException("Invalid token"); }

            return Convert.ToInt32(match.Groups["Id"].Value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Compares the expiry time to expiry rules
        /// </summary>
        public bool HasExpired(DateTime expires) => expires < DateTime.UtcNow;

        /// <summary>
        /// Value to reset the expiry to when a token is used successfully
        /// </summary>
        public DateTime ResetExpiryTo() => DateTime.UtcNow;
    }
}