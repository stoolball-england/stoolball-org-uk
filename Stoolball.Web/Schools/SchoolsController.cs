using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Data.Abstractions;
using Stoolball.Listings;
using Stoolball.Schools;
using Stoolball.Web.Navigation;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Schools
{
    public class SchoolsController : RenderController, IRenderControllerAsync
    {
        private readonly ISchoolDataSource _schoolDataSource;
        private readonly IListingsModelBuilder<School, SchoolFilter, SchoolsViewModel> _listingsModelBuilder;

        public SchoolsController(ILogger<SchoolsController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            ISchoolDataSource schoolDataSource,
            IListingsModelBuilder<School, SchoolFilter, SchoolsViewModel> listingsModelBuilder)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _schoolDataSource = schoolDataSource ?? throw new ArgumentNullException(nameof(schoolDataSource));
            _listingsModelBuilder = listingsModelBuilder ?? throw new ArgumentNullException(nameof(listingsModelBuilder));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var model = await _listingsModelBuilder.BuildModel(
                () => new SchoolsViewModel(CurrentPage),
                _schoolDataSource.ReadTotalSchools,
                _schoolDataSource.ReadSchools,
                Constants.Pages.Schools,
                new Uri(Request.GetEncodedUrl()),
                Request.QueryString.Value);

            model.Breadcrumbs.RemoveAt(model.Breadcrumbs.Count - 1);
            model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Schools, Url = new Uri(Constants.Pages.SchoolsUrl, UriKind.Relative) });

            return CurrentTemplate(model);
        }
    }
}