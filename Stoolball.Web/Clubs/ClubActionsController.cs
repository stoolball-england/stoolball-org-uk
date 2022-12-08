using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Clubs;
using Stoolball.Data.Abstractions;
using Stoolball.Security;
using Stoolball.Web.Navigation;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Clubs
{
    public class ClubActionsController : RenderController, IRenderControllerAsync
    {
        private readonly IClubDataSource _clubDataSource;
        private readonly IAuthorizationPolicy<Club> _authorizationPolicy;

        public ClubActionsController(ILogger<ClubActionsController> logger,
           ICompositeViewEngine compositeViewEngine,
           IUmbracoContextAccessor umbracoContextAccessor,
           IClubDataSource clubDataSource,
           IAuthorizationPolicy<Club> authorizationPolicy)
           : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _clubDataSource = clubDataSource ?? throw new ArgumentNullException(nameof(clubDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var model = new ClubViewModel(CurrentPage)
            {
                Club = await _clubDataSource.ReadClubByRoute(Request.Path)
            };

            if (model.Club == null)
            {
                return NotFound();
            }
            else
            {
                model.Authorization.AuthorizationFor = model.Club.ClubName;
                model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(model.Club);
                model.Authorization.AuthorizedMemberNames = await _authorizationPolicy.AuthorizedMemberNames(model.Club);
                model.Authorization.AuthorizedGroupNames = _authorizationPolicy.AuthorizedGroupNames(model.Club);

                model.Metadata.PageTitle = "Edit " + model.Club.ClubName;

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Teams, Url = new Uri(Constants.Pages.TeamsUrl, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Club.ClubName, Url = new Uri(model.Club.ClubRoute, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}