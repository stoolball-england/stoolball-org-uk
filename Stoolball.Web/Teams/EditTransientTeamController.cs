using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Data.Abstractions;
using Stoolball.Dates;
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
    public class EditTransientTeamController : RenderController, IRenderControllerAsync
    {
        private readonly ITeamDataSource _teamDataSource;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IAuthorizationPolicy<Team> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateFormatter;

        public EditTransientTeamController(ILogger<EditTransientTeamController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            ITeamDataSource teamDataSource,
            IMatchListingDataSource matchDataSource,
            IAuthorizationPolicy<Team> authorizationPolicy,
            IDateTimeFormatter dateFormatter)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
        }

        [HttpGet]
        [ContentSecurityPolicy(TinyMCE = true, Forms = true)]
        public async new Task<IActionResult> Index()
        {
            var model = new TeamViewModel(CurrentPage)
            {
                Team = await _teamDataSource.ReadTeamByRoute(Request.Path, true).ConfigureAwait(false)
            };


            if (model.Team == null)
            {
                return NotFound();
            }
            else
            {
                model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(model.Team);

                model.Matches = new MatchListingViewModel(CurrentPage)
                {
                    Matches = await _matchDataSource.ReadMatchListings(new MatchFilter
                    {
                        TeamIds = new List<Guid> { model.Team.TeamId!.Value },
                        IncludeMatches = false
                    }, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false)
                };

                var match = model.Matches.Matches.First();
                model.Metadata.PageTitle = $"Edit {model.Team.TeamName}, {_dateFormatter.FormatDate(match.StartTime, false, false)}";

                model.Breadcrumbs.Add(new Breadcrumb { Name = match.MatchName, Url = new Uri(match.MatchRoute, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}