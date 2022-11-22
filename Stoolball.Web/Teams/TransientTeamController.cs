using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Data.Abstractions;
using Stoolball.Dates;
using Stoolball.Email;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Stoolball.Web.Teams.Models;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Teams
{
    public class TransientTeamController : RenderController, IRenderControllerAsync
    {
        private readonly ITeamDataSource _teamDataSource;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IAuthorizationPolicy<Team> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateFormatter;
        private readonly IEmailProtector _emailProtector;

        public TransientTeamController(ILogger<TransientTeamController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            ITeamDataSource teamDataSource,
            IMatchListingDataSource matchDataSource,
            IAuthorizationPolicy<Team> authorizationPolicy,
            IDateTimeFormatter dateFormatter,
            IEmailProtector emailProtector)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
            _emailProtector = emailProtector ?? throw new ArgumentNullException(nameof(emailProtector));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var model = new TeamViewModel(CurrentPage)
            {
                Team = await _teamDataSource.ReadTeamByRoute(Request.Path, true)
            };

            if (model.Team == null)
            {
                return NotFound();
            }

            model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(model.Team);

            model.Matches = new MatchListingViewModel(CurrentPage)
            {
                Matches = await _matchDataSource.ReadMatchListings(new MatchFilter
                {
                    TeamIds = new List<Guid> { model.Team.TeamId!.Value },
                    IncludeMatches = false
                }, MatchSortOrder.MatchDateEarliestFirst),
                HighlightNextMatch = false
            };

            var match = model.Matches.Matches.First();
            model.Metadata.PageTitle = model.Team.TeamNameAndPlayerType() + ", " + _dateFormatter.FormatDate(match.StartTime, false, false);

            model.Team.Cost = _emailProtector.ProtectEmailAddresses(model.Team.Cost, User.Identity?.IsAuthenticated ?? false);
            model.Team.Introduction = _emailProtector.ProtectEmailAddresses(model.Team.Introduction, User.Identity?.IsAuthenticated ?? false);
            model.Team.PublicContactDetails = _emailProtector.ProtectEmailAddresses(model.Team.PublicContactDetails, User.Identity?.IsAuthenticated ?? false);

            model.Breadcrumbs.Add(new Breadcrumb { Name = match.MatchName, Url = new Uri(match.MatchRoute, UriKind.Relative) });

            return CurrentTemplate(model);
        }
    }
}
