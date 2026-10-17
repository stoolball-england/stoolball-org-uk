using System.Linq;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Stoolball.Web.Routing;
using Xunit;

namespace Stoolball.Web.UnitTests.Routing
{
    public class StoolballRouterControllerTests
    {
        [Fact]
        public void Index_is_the_only_action_and_accepts_only_GET()
        {
            // If the synchronous Index action inherited from RenderController is also discovered, it handles any request
            // that is not a GET (eg HEAD) and throws a ModelBindingException.
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddControllers().ConfigureApplicationPartManager(manager =>
            {
                manager.ApplicationParts.Clear();
                manager.ApplicationParts.Add(new AssemblyPart(typeof(StoolballRouterController).Assembly));
            });
            using var serviceProvider = services.BuildServiceProvider();

            var actions = serviceProvider.GetRequiredService<IActionDescriptorCollectionProvider>().ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .Where(x => x.ControllerTypeInfo == typeof(StoolballRouterController))
                .ToList();

            var action = Assert.Single(actions);
            Assert.Equal("Index", action.ActionName);
            var httpMethods = action.ActionConstraints?.OfType<HttpMethodActionConstraint>().SelectMany(x => x.HttpMethods).ToList();
            Assert.Equal(["GET"], httpMethods);
        }
    }
}
