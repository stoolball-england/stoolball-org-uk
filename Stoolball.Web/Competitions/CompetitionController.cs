using Stoolball.Email;
using Stoolball.Umbraco.Data.Competitions;
using Stoolball.Web.Routing;
using System;
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
    public class CompetitionController : RenderMvcControllerAsync
    {
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly IEmailProtector _emailProtector;

        public CompetitionController(IGlobalSettings globalSettings,
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

            var model = new CompetitionViewModel(contentModel.Content)
            {
                Competition = await _seasonDataSource.ReadCompetitionByRoute(Request.Url.AbsolutePath).ConfigureAwait(false)
            };

            if (model.Competition == null)
            {
                return new HttpNotFoundResult();
            }
            else if (model.Competition.Seasons.Count > 0)
            {
                Response.AddHeader("Location", new Uri(Request.Url, new Uri(model.Competition.Seasons[0].SeasonRoute, UriKind.Relative)).ToString());
                return new HttpStatusCodeResult(303);
            }
            else
            {
                model.Metadata.PageTitle = model.Competition.CompetitionName;
                model.Metadata.Description = model.Competition.Description();

                model.Competition.Introduction = _emailProtector.ProtectEmailAddresses(model.Competition.Introduction, User.Identity.IsAuthenticated);
                model.Competition.PublicContactDetails = _emailProtector.ProtectEmailAddresses(model.Competition.PublicContactDetails, User.Identity.IsAuthenticated);

                return CurrentTemplate(model);
            }
        }
    }
}