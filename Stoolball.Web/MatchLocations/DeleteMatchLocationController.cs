using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Security;
using Stoolball.Web.MatchLocations.Models;
using Stoolball.Web.Navigation;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.MatchLocations
{
    public class DeleteMatchLocationController : RenderController, IRenderControllerAsync
    {
        private readonly IMatchLocationDataSource _matchLocationDataSource;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IAuthorizationPolicy<MatchLocation> _authorizationPolicy;

        public DeleteMatchLocationController(ILogger<DeleteMatchLocationController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IMatchLocationDataSource matchLocationDataSource,
            IMatchListingDataSource matchDataSource,
            IAuthorizationPolicy<MatchLocation> authorizationPolicy)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _matchLocationDataSource = matchLocationDataSource ?? throw new ArgumentNullException(nameof(matchLocationDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true)]
        public async new Task<IActionResult> Index()
        {
            var model = new DeleteMatchLocationViewModel(CurrentPage)
            {
                MatchLocation = await _matchLocationDataSource.ReadMatchLocationByRoute(Request.Path, true),
            };

            if (model.MatchLocation == null)
            {
                return NotFound();
            }
            else
            {
                model.TotalMatches = await _matchDataSource.ReadTotalMatches(new MatchFilter
                {
                    MatchLocationIds = new List<Guid> { model.MatchLocation.MatchLocationId!.Value },
                    IncludeTournamentMatches = true
                });
                model.ConfirmDeleteRequest.RequiredText = model.MatchLocation.NameAndLocalityOrTownIfDifferent();

                model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(model.MatchLocation);

                model.Metadata.PageTitle = "Delete " + model.MatchLocation.NameAndLocalityOrTown();

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.MatchLocations, Url = new Uri(Constants.Pages.MatchLocationsUrl, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.MatchLocation.NameAndLocalityOrTownIfDifferent(), Url = new Uri(model.MatchLocation.MatchLocationRoute, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}