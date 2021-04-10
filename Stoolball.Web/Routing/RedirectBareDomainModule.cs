using System.Web;

namespace Stoolball.Web.Routing
{
    /// <summary>
    /// Redirect stoolball.org.uk to www.stoolball.org.uk because IISRewrite can't do it. This appears to be because CloudFlare has already rewritten the hostname that IIS sees to www.stoolball.org.uk.
    /// </summary>
    public class RedirectBareDomainModule : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            context.BeginRequest += Context_BeginRequest;
        }

        private void Context_BeginRequest(object sender, System.EventArgs e)
        {
            var context = (HttpApplication)sender;
            if (context.Request.Url.Host.ToUpperInvariant() == "STOOLBALL.ORG.UK")
            {
                context.Response.RedirectPermanent("https://www.stoolball.org.uk" + context.Request.RawUrl, true);
            }
        }

        public void Dispose()
        {
        }
    }
}
