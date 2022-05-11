using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Matches
{
    public class EditFriendlyMatchController : RenderController, IRenderControllerAsync
    {
        private readonly IMatchDataSource _matchDataSource;
        private readonly IAuthorizationPolicy<Match> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateFormatter;
        private readonly ISeasonDataSource _seasonDataSource;

        public EditFriendlyMatchController(ILogger<EditFriendlyMatchController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IMatchDataSource matchDataSource,
            ISeasonDataSource seasonDataSource,
            IAuthorizationPolicy<Match> authorizationPolicy,
            IDateTimeFormatter dateFormatter)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true, TinyMCE = true)]
        public async new Task<IActionResult> Index()
        {
            var model = new EditFriendlyMatchViewModel(CurrentPage)
            {
                Match = await _matchDataSource.ReadMatchByRoute(Request.Path)
            };

            if (model.Match == null)
            {
                return NotFound();
            }
            else
            {
                // This page is only for matches in the future
                if (model.Match.StartTime <= DateTime.UtcNow)
                {
                    return NotFound();
                }

                model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(model.Match);

                if (model.Match.Season != null)
                {
                    model.Match.Season = await _seasonDataSource.ReadSeasonByRoute(model.Match.Season.SeasonRoute, true);
                    model.SeasonFullName = model.Match.Season.SeasonFullName();
                }

                model.MatchDate = model.Match.StartTime;
                if (model.Match.StartTimeIsKnown)
                {
                    model.StartTime = model.Match.StartTime;
                }
                model.HomeTeamId = model.Match.Teams.SingleOrDefault(x => x.TeamRole == TeamRole.Home)?.Team.TeamId;
                model.HomeTeamName = model.Match.Teams.SingleOrDefault(x => x.TeamRole == TeamRole.Home)?.Team.TeamName;
                model.AwayTeamId = model.Match.Teams.SingleOrDefault(x => x.TeamRole == TeamRole.Away)?.Team.TeamId;
                model.AwayTeamName = model.Match.Teams.SingleOrDefault(x => x.TeamRole == TeamRole.Away)?.Team.TeamName;
                model.MatchLocationId = model.Match.MatchLocation?.MatchLocationId;
                model.MatchLocationName = model.Match.MatchLocation?.NameAndLocalityOrTownIfDifferent();

                model.Metadata.PageTitle = "Edit " + model.Match.MatchFullName(x => _dateFormatter.FormatDate(x, false, false, false));

                if (model.Match.Season != null)
                {
                    model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
                    model.Breadcrumbs.Add(new Breadcrumb { Name = model.Match.Season.Competition.CompetitionName, Url = new Uri(model.Match.Season.Competition.CompetitionRoute, UriKind.Relative) });
                    model.Breadcrumbs.Add(new Breadcrumb { Name = model.Match.Season.SeasonName(), Url = new Uri(model.Match.Season.SeasonRoute, UriKind.Relative) });
                }
                else
                {
                    model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Matches, Url = new Uri(Constants.Pages.MatchesUrl, UriKind.Relative) });
                }
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Match.MatchName, Url = new Uri(model.Match.MatchRoute, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}