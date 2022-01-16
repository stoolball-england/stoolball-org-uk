using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Clubs;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Stoolball.Web.Clubs
{
    public class ClubController : RenderMvcControllerAsync
    {
        private readonly IClubDataSource _clubDataSource;
        private readonly IAuthorizationPolicy<Club> _authorizationPolicy;

        public ClubController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IClubDataSource clubDataSource,
           IAuthorizationPolicy<Club> authorizationPolicy)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _clubDataSource = clubDataSource ?? throw new System.ArgumentNullException(nameof(clubDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new System.ArgumentNullException(nameof(authorizationPolicy));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var model = new ClubViewModel(contentModel.Content, Services?.UserService)
            {
                Club = await _clubDataSource.ReadClubByRoute(Request.RawUrl).ConfigureAwait(false)
            };

            if (model.Club == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.IsAuthorized = _authorizationPolicy.IsAuthorized(model.Club);

                model.Metadata.PageTitle = model.Club.ClubName;
                model.Metadata.Description = model.Club.Description();

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Teams, Url = new Uri(Constants.Pages.TeamsUrl, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}