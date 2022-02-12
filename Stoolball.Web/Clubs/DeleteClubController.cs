using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Clubs;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Clubs
{
    public class DeleteClubController : RenderController, IRenderControllerAsync
    {
        private readonly IClubDataSource _clubDataSource;
        private readonly IAuthorizationPolicy<Club> _authorizationPolicy;

        public DeleteClubController(ILogger<DeleteClubController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IClubDataSource clubDataSource,
            IAuthorizationPolicy<Club> authorizationPolicy)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _clubDataSource = clubDataSource ?? throw new System.ArgumentNullException(nameof(clubDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new System.ArgumentNullException(nameof(authorizationPolicy));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true)]
        public async new Task<IActionResult> Index()
        {
            var model = new DeleteClubViewModel(CurrentPage)
            {
                Club = await _clubDataSource.ReadClubByRoute(Request.Path).ConfigureAwait(false)
            };

            if (model.Club == null)
            {
                return NotFound();
            }
            else
            {
                model.ConfirmDeleteRequest.RequiredText = model.Club.ClubName;

                model.IsAuthorized = await _authorizationPolicy.IsAuthorized(model.Club);

                model.Metadata.PageTitle = "Delete " + model.Club.ClubName;

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Teams, Url = new Uri(Constants.Pages.TeamsUrl, UriKind.Relative) });
                if (!model.Deleted)
                {
                    model.Breadcrumbs.Add(new Breadcrumb { Name = model.Club.ClubName, Url = new Uri(model.Club.ClubRoute, UriKind.Relative) });
                }

                return CurrentTemplate(model);
            }
        }
    }
}