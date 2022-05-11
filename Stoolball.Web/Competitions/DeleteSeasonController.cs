using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.Competitions.Models;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Competitions
{
    public class DeleteSeasonController : RenderController, IRenderControllerAsync
    {
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;

        public DeleteSeasonController(ILogger<DeleteSeasonController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            ISeasonDataSource seasonDataSource,
            IMatchListingDataSource matchDataSource,
            IAuthorizationPolicy<Competition> authorizationPolicy)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true)]
        public async new Task<IActionResult> Index()
        {
            var model = new DeleteSeasonViewModel(CurrentPage)
            {
                Season = await _seasonDataSource.ReadSeasonByRoute(Request.Path, true).ConfigureAwait(false)
            };

            if (model.Season == null)
            {
                return NotFound();
            }
            else
            {
                model.TotalMatches = await _matchDataSource.ReadTotalMatches(new MatchFilter
                {
                    SeasonIds = new List<Guid> { model.Season.SeasonId!.Value },
                    IncludeTournamentMatches = true
                });

                model.ConfirmDeleteRequest.RequiredText = model.Season.SeasonFullName();

                model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(model.Season.Competition);

                model.Metadata.PageTitle = "Delete " + model.Season.SeasonFullNameAndPlayerType();

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Season.Competition.CompetitionName, Url = new Uri(model.Season.Competition.CompetitionRoute, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Season.SeasonName(), Url = new Uri(model.Season.SeasonRoute, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}