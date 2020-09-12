using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Competitions;
using Stoolball.Email;
using Stoolball.Security;
using Stoolball.Umbraco.Data.Competitions;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Stoolball.Web.Competitions
{
    public class SeasonController : RenderMvcControllerAsync
    {
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;
        private readonly IEmailProtector _emailProtector;

        public SeasonController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ISeasonDataSource seasonDataSource,
           IAuthorizationPolicy<Competition> authorizationPolicy,
           IEmailProtector emailProtector)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _seasonDataSource = seasonDataSource ?? throw new System.ArgumentNullException(nameof(seasonDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new System.ArgumentNullException(nameof(authorizationPolicy));
            _emailProtector = emailProtector ?? throw new System.ArgumentNullException(nameof(emailProtector));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var model = new SeasonViewModel(contentModel.Content, Services?.UserService)
            {
                Season = await _seasonDataSource.ReadSeasonByRoute(Request.RawUrl, true).ConfigureAwait(false)
            };

            if (model.Season == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.IsAuthorized = _authorizationPolicy.IsAuthorized(model.Season.Competition);

                model.Metadata.PageTitle = model.Season.SeasonFullNameAndPlayerType();
                model.Metadata.Description = model.Season.Description();

                model.Season.Competition.Introduction = _emailProtector.ProtectEmailAddresses(model.Season.Competition.Introduction, User.Identity.IsAuthenticated);
                model.Season.Competition.PublicContactDetails = _emailProtector.ProtectEmailAddresses(model.Season.Competition.PublicContactDetails, User.Identity.IsAuthenticated);

                model.Season.Introduction = _emailProtector.ProtectEmailAddresses(model.Season.Introduction, User.Identity.IsAuthenticated);

                return CurrentTemplate(model);
            }
        }
    }
}