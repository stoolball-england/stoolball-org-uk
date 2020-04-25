using Stoolball.Email;
using Stoolball.Umbraco.Data.Competitions;
using Stoolball.Web.Routing;
using System.Threading.Tasks;
using System.Web.Mvc;
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
        private readonly IEmailProtector _emailProtector;

        public SeasonController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ISeasonDataSource seasonDataSource,
           IEmailProtector emailProtector)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _seasonDataSource = seasonDataSource ?? throw new System.ArgumentNullException(nameof(seasonDataSource));
            _emailProtector = emailProtector ?? throw new System.ArgumentNullException(nameof(emailProtector));
        }

        [HttpGet]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var model = new SeasonViewModel(contentModel.Content)
            {
                Season = await _seasonDataSource.ReadSeasonByRoute(Request.Url.AbsolutePath, true).ConfigureAwait(false)
            };

            if (model.Season == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.Metadata.PageTitle = model.Season.SeasonFullNameAndPlayerType();
                model.Metadata.Description = model.Season.Description();

                model.Season.Competition.Introduction = _emailProtector.ProtectEmailAddresses(model.Season.Competition.Introduction, User.Identity.IsAuthenticated);
                model.Season.Competition.PublicContactDetails = _emailProtector.ProtectEmailAddresses(model.Season.Competition.PublicContactDetails, User.Identity.IsAuthenticated);

                model.Season.Introduction = _emailProtector.ProtectEmailAddresses(model.Season.Introduction, User.Identity.IsAuthenticated);
                model.Season.Results = _emailProtector.ProtectEmailAddresses(model.Season.Results, User.Identity.IsAuthenticated);

                return CurrentTemplate(model);
            }
        }
    }
}