namespace Stoolball.Web.Routing
{
    /// <summary>
    /// Recognises alternate domains, which are always redirected to the preferred domain
    /// </summary>
    public static class PreferredDomain
    {
        public static bool IsAlternateDomain(string host)
        {
            var domain = host.ToUpperInvariant();
            return domain == "STOOLBALL.ORG.UK" || domain == "STOOLBALL.CO.UK" || domain == "WWW.STOOLBALL.CO.UK";
        }
    }
}
