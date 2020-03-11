using System;

namespace Stoolball.Security
{
    /// <summary>
    /// A verification token in the format {id}-{guid}
    /// </summary>
    public interface IVerificationToken
    {
        /// <summary>
        /// Extracts the id part of a token
        /// </summary>
        int ExtractId(string token);

        /// <summary>
        /// Generates a new token based on the supplied id
        /// </summary>
        (string token, DateTime expires) TokenFor(int id);

        /// <summary>
        /// Compares the expiry time to expiry rules
        /// </summary>
        bool HasExpired(DateTime expires);

        /// <summary>
        /// Value to reset the expiry to when a token is used successfully
        /// </summary>
        DateTime ResetExpiryTo();
    }
}