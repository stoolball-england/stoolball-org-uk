using System.Net.Http.Headers;
using System.Web.Http;
using Umbraco.Core.Composing;

namespace Stoolball.Web.WebApi
{
    public class WebApiComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            GlobalConfiguration.Configuration.MapHttpAttributeRoutes();
            GlobalConfiguration.Configuration.Formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html")); // JSON in the browser
        }
    }
}