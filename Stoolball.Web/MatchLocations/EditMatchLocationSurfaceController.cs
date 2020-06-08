using Stoolball.MatchLocations;
using Stoolball.Umbraco.Data.MatchLocations;
using Stoolball.Web.Security;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Web.MatchLocations
{
    public class EditMatchLocationSurfaceController : SurfaceController
    {
        private readonly IMatchLocationDataSource _matchLocationDataSource;
        private readonly IMatchLocationRepository _matchLocationRepository;

        public EditMatchLocationSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, IMatchLocationDataSource matchLocationDataSource, IMatchLocationRepository matchLocationRepository)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _matchLocationDataSource = matchLocationDataSource;
            _matchLocationRepository = matchLocationRepository ?? throw new System.ArgumentNullException(nameof(matchLocationRepository));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(GoogleMaps = true, TinyMCE = true, Forms = true)]
        public async Task<ActionResult> UpdateMatchLocation([Bind(Prefix = "MatchLocation", Include = "SecondaryAddressableObjectName,PrimaryAddressableObjectName,StreetDescription,Locality,Town,AdministrativeArea,Postcode,GeoPrecision,Latitude,Longitude")] MatchLocation location)
        {
            if (location is null)
            {
                throw new System.ArgumentNullException(nameof(location));
            }

            var beforeUpdate = await _matchLocationDataSource.ReadMatchLocationByRoute(Request.RawUrl).ConfigureAwait(false);
            location.MatchLocationId = beforeUpdate.MatchLocationId;
            location.MatchLocationRoute = beforeUpdate.MatchLocationRoute;

            // get this from the unvalidated form instead of via modelbinding so that HTML can be allowed
            location.MatchLocationNotes = Request.Unvalidated.Form["MatchLocation.MatchLocationNotes"];

            var isAuthorized = Members.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.Editors, beforeUpdate.MemberGroupName }, null);

            if (isAuthorized && ModelState.IsValid)
            {
                var currentMember = Members.GetCurrentMember();
                await _matchLocationRepository.UpdateMatchLocation(location, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                // redirect back to the location actions page that led here
                return Redirect(location.MatchLocationRoute + "/edit");
            }

            var viewModel = new
                MatchLocationViewModel(CurrentPage)
            {
                MatchLocation = location,
                IsAuthorized = isAuthorized
            };
            viewModel.Metadata.PageTitle = $"Edit {location.NameAndLocalityOrTown()}";
            return View("EditMatchLocation", viewModel);
        }
    }
}