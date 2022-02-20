using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Listings;
using Stoolball.Navigation;
using Stoolball.Schools;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Stoolball.Web.Schools
{
    public class SchoolsController : RenderMvcControllerAsync
    {
        private readonly ISchoolDataSource _schoolDataSource;
        private readonly IListingsModelBuilder<School, SchoolFilter, SchoolsViewModel> _listingsModelBuilder;

        public SchoolsController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ISchoolDataSource schoolDataSource,
           IListingsModelBuilder<School, SchoolFilter, SchoolsViewModel> listingsModelBuilder)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _schoolDataSource = schoolDataSource ?? throw new System.ArgumentNullException(nameof(schoolDataSource));
            _listingsModelBuilder = listingsModelBuilder ?? throw new System.ArgumentNullException(nameof(listingsModelBuilder));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var model = await _listingsModelBuilder.BuildModel(
                () => new SchoolsViewModel(contentModel.Content, Services?.UserService),
                _schoolDataSource.ReadTotalSchools,
                _schoolDataSource.ReadSchools,
                Constants.Pages.Schools,
                Request.Url,
                Request.Url.Query).ConfigureAwait(false);

            model.Breadcrumbs.RemoveAt(model.Breadcrumbs.Count - 1);
            model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Schools, Url = new Uri(Constants.Pages.SchoolsUrl, UriKind.Relative) });

            return CurrentTemplate(model);
        }
    }
}