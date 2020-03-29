# Routing for stoolball pages

Umbraco routes URLs for its content nodes automatically. However, Umbraco Cloud has a limit of 500 content nodes, so it's useful to limit those taken up by stoolball entities. It's also useful to have access to the Umbraco context when displaying a stoolball entity.

The solution to this is to route all stoolball entity URLs (clubs, teams and matches and so on) through one Umbraco content node, based on the 'Stoolball router' document type. This content node must be created at the root of the Content section in the Umbraco back office.

## How stoolball URLs are routed to a controller

In the `Stoolball.Web.Routing` namespace `ContentFinderComposer` registers `StoolballRouteContentFinder` to run before Umbraco's default content finder. This means that if someone creates an Umbraco page that conflicts with a route reserved for a type of stoolball entity, the stoolball route wins.

When `StoolballRouteContentFinder` recognises a route reserved for a stoolball entity it sets the 'Stoolball router' content node to respond, and passes it the type of stoolball route it recognised via an HTTP response header. `StoolballRouterController` takes over, matching the header value to a controller appropriate for the type of stoolball entity, and passes responsibility for the response entirely to that controller.

## Create a model, view and controller for a stoolball entity type

Responses for stoolball entities are designed to be as similar to a standard Umbraco response as possible. They have access to the Umbraco context as well as the opportunity to read their data from the custom tables in the Umbraco database.

The **model** should inherit from `Stoolball.Web.Routing.BaseViewModel`. This requires a constructor that accepts an `IPublishedContent` but is otherwise a normal view model.

```csharp
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;

namespace Stoolball.Web.Example
{
    public class ExampleViewModel : BaseViewModel
    {
        public ExampleViewModel(IPublishedContent contentModel) : base(contentModel)
        {
        }

        /// ...other properties here
    }
}
```

The **view** is a normal Umbraco view:

```razor
@inherits Umbraco.Web.Mvc.UmbracoViewPage<Stoolball.Web.Example.ExampleViewModel>
```

The **controller** should inherit from `Stoolball.Web.Routing.RenderMvcControllerAsync` and implement its abstract `Index` method. It has access to all the usual Umbraco controller properties, such as the `UmbracoHelper`, `MembershipHelper` and `ServiceContext`. It must name the required view rather than using `CurrentTemplate(model)` as an Umbraco controller usually would:

```csharp
using Stoolball.Web.Routing;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Web.Models;

namespace Stoolball.Web.Example
{
    public class ExampleController : RenderMvcControllerAsync
    {
        [HttpGet]
        public override async Task<ActionResult> Index(ContentModel contentModel)
        {
            var model = new ExampleViewModel(contentModel.Content);

            // ...use `await` to fetch data from the database asynchonously

            return View("Club", model);
        }
    }
}
```

Finally, you need to wire up this controller to be served via `StoolballRouterController` as described above:

1. Register the controller type in `Stoolball.Web.DependencyInjectionComposer`:

   ```csharp
   public void Compose(Composition composition)
   {
       /// ...other registrations

       composition.Register<ExampleController>(Lifetime.Request);
   }
   ```

2. Add a new value to the `Stoolball.Web.Routing.StoolballRouteType` enum.

3. Update `StoolballRouteContentFinder` to recognise the new route, and return the new `StoolballRouteType` in its HTTP header.

4. Add the new `StoolballRouteType` value and its matching controller type to the dictionary in `StoolballRouterController`:

   ```csharp
    private readonly Dictionary<StoolballRouteType, Type> _supportedControllers = new Dictionary<StoolballRouteType, Type> {

           // ...other route types and controllers

           { StoolballRouteType.Example, typeof(ExampleController) }
       };
   ```

5. Create a template in the Umbraco back office matching the new view, and set it as an allowed template on the 'Stoolball router' document type. This is optional as it works without, but it makes the link between the two clearer when looking in the back office.
