# Permissions

Permissions to modify entities in the [stoolball data model](DataModel.md) are managed using Umbraco's membership feature set.

Each `Club`, `Team`, `School`, `MatchLocation` and `Competition` has a Umbraco Member Group, which is identified by its name and numeric id in the tables for those entities. The group GUID/UDI is not used because the membership APIs do not support lookups using that yet. Each `Match` and `Tournament` records the GUID/UDI of the individual Umbraco Member that created it. There is no group for these, only because it would generate thousands more groups and the Umbraco Back Office UI would be too slow.

Other entities get their permissions from these ones. For example, a `PlayerIdentity` is considered part of its `Team` and a `Season` is considered part of its `Competition`.

To check whether the current Member has permission to one of these entities, inject an `IAuthorizationPolicy<T>` into your controller and call its `IsAuthorized` method. Keeping permissions checks inside an `IAuthorizationPolicy<T>` means that the same check can be repeated on multiple pages and always use the same rules, and that check can be substituted in tests.

For example:

```csharp
public class CreateCompetitionController : RenderMvcControllerAsync
{
    private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;

    public CreateCompetitionController(IGlobalSettings globalSettings,
        IUmbracoContextAccessor umbracoContextAccessor,
        ServiceContext serviceContext,
        AppCaches appCaches,
        IProfilingLogger profilingLogger,
        UmbracoHelper umbracoHelper,
        IAuthorizationPolicy<Competition> authorizationPolicy)
        : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
    {
        _authorizationPolicy = authorizationPolicy;
    }

    [HttpGet]
    public async override Task<ActionResult> Index(ContentModel contentModel)
    {
        var model = new CompetitionViewModel(contentModel.Content)
        {
            Competition = new Competition()
        };

        model.IsAuthorized = _authorizationPolicy.IsAuthorized(model.Competition, Members);
    }
}
```

This will populate `model.IsAuthorized` with boolean values for relevant permissions, so you can check:

```csharp
using Umbraco.Web.Security;

...

if (model.IsAuthorized[AuthorizedAction.CreateCompetition]) {
    // the current member is authorized
}
```

Permissions which require a check against the Member Group assigned to the entity require the group name to be populated from the database first. In the above example if the permission to be checked were `AuthorizedAction.EditCompetition`, then `model.Competition.MemberGroupName` would need to be populated before calling `_authorizationPolicy.IsAuthorized(model.Competition, Members)`.
