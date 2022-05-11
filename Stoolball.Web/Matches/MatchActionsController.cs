using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
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
    public class MatchActionsController : RenderController, IRenderControllerAsync
    {
        private readonly IMatchDataSource _matchDataSource;
        private readonly IAuthorizationPolicy<Match> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateFormatter;

        public MatchActionsController(ILogger<MatchActionsController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IMatchDataSource matchDataSource,
            IAuthorizationPolicy<Match> authorizationPolicy,
            IDateTimeFormatter dateFormatter)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var model = new MatchViewModel(CurrentPage)
            {
                Match = await _matchDataSource.ReadMatchByRoute(Request.Path)
            };

            if (model.Match == null)
            {
                return NotFound();
            }
            else
            {
                model.Authorization.AuthorizedAction = "edit or delete";
                model.Authorization.AuthorizationFor = "this match";
                model.Authorization.Note = model.Match.StartTime <= DateTime.UtcNow && model.Match.Tournament == null ? "Anyone can edit the result." : null;
                model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(model.Match);
                model.Authorization.AuthorizedMemberNames = await _authorizationPolicy.AuthorizedMemberNames(model.Match);

                model.Metadata.PageTitle = model.Match.MatchFullName(x => _dateFormatter.FormatDate(x, false, false, false)) + " - stoolball match";

                if (model.Match.Season != null)
                {
                    model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
                    model.Breadcrumbs.Add(new Breadcrumb { Name = model.Match.Season.Competition.CompetitionName, Url = new Uri(model.Match.Season.Competition.CompetitionRoute, UriKind.Relative) });
                    model.Breadcrumbs.Add(new Breadcrumb { Name = model.Match.Season.SeasonName(), Url = new Uri(model.Match.Season.SeasonRoute, UriKind.Relative) });
                }
                else if (model.Match.Tournament != null)
                {
                    model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Tournaments, Url = new Uri(Constants.Pages.TournamentsUrl, UriKind.Relative) });
                    model.Breadcrumbs.Add(new Breadcrumb { Name = model.Match.Tournament.TournamentName, Url = new Uri(model.Match.Tournament.TournamentRoute, UriKind.Relative) });
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