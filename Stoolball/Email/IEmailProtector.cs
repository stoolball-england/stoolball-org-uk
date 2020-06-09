namespace Stoolball.Email
{
    public interface IEmailProtector
    {
        /// <summary>
        /// Links email addresses in HTML, but protecting them from unauthenticated users
        /// </summary>
        /// <param name="html">An HTML fragment</param>
        /// <param name="userIsAuthenticated"></param>
        /// <param name="excludedAddress">An address which may be obfuscated but may not be entirely hidden</param>
        /// <returns>Updated HTML</returns>
        string ProtectEmailAddresses(string html, bool userIsAuthenticated, string excludedAddress);

        // <summary>
        /// Links email addresses in HTML, but protecting them from unauthenticated users
        /// </summary>
        /// <param name="html">An HTML fragment</param>
        /// <param name="userIsAuthenticated"></param>
        /// <returns>Updated HTML</returns>
        string ProtectEmailAddresses(string html, bool userIsAuthenticated);
    }
}