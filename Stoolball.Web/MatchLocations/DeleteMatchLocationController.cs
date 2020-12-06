using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Matches;
using Stoolball.MatchLocations;
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

namespace Stoolball.Web.MatchLocations
{
    public class DeleteMatchLocationController : RenderMvcControllerAsync
    {
        private readonly IMatchLocationDataSource _matchLocationDataSource;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IAuthorizationPolicy<MatchLocation> _authorizationPolicy;

        public DeleteMatchLocationController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IMatchLocationDataSource matchLocationDataSource,
           IMatchListingDataSource matchDataSource,
           IAuthorizationPolicy<MatchLocation> authorizationPolicy)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _matchLocationDataSource = matchLocationDataSource ?? throw new System.ArgumentNullException(nameof(matchLocationDataSource));
            _matchDataSource = matchDataSource ?? throw new System.ArgumentNullException(nameof(matchDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true)]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var model = new DeleteMatchLocationViewModel(contentModel.Content, Services?.UserService)
            {
                MatchLocation = await _matchLocationDataSource.ReadMatchLocationByRoute(Request.RawUrl, true).ConfigureAwait(false),
            };

            if (model.MatchLocation == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.TotalMatches = await _matchDataSource.ReadTotalMatches(new MatchQuery
                {
                    MatchLocationIds = new List<Guid> { model.MatchLocation.MatchLocationId.Value },
                    IncludeTournamentMatches = true
                }).ConfigureAwait(false);
                model.ConfirmDeleteRequest.RequiredText = model.MatchLocation.Name();

                model.IsAuthorized = _authorizationPolicy.IsAuthorized(model.MatchLocation);

                model.Metadata.PageTitle = "Delete " + model.MatchLocation.NameAndLocalityOrTown();

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.MatchLocations, Url = new Uri(Constants.Pages.MatchLocationsUrl, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.MatchLocation.NameAndLocalityOrTown(), Url = new Uri(model.MatchLocation.MatchLocationRoute, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}