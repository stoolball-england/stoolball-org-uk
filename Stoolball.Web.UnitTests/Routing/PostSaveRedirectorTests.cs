using Stoolball.Web.Routing;
using Xunit;

namespace Stoolball.Web.Tests.Routing
{
    public class PostSaveRedirectorTests
    {
        private const string ROUTE_BEFORE = "/route-before";
        private const string ROUTE_AFTER = "/route-after";
        private const string DESTINATION_SUFFIX = "/edit";

        [Theory]
        [InlineData(null)]
        [InlineData("https://localhost" + ROUTE_BEFORE + DESTINATION_SUFFIX)]
        public void WorkOutRedirect_Goes_To_Referrer_Or_New_Route(string referrer)
        {
            var redirector = new PostSaveRedirector();

            var result = redirector.WorkOutRedirect(ROUTE_BEFORE, ROUTE_AFTER, DESTINATION_SUFFIX, referrer, null);

            Assert.Equal(ROUTE_AFTER + DESTINATION_SUFFIX, result.Url);
        }
    }
}
