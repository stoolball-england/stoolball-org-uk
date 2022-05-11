using System;
using System.Collections.Generic;
using System.Linq;
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
    public class EditCloseOfPlayController : RenderController, IRenderControllerAsync
    {
        private readonly IMatchDataSource _matchDataSource;
        private readonly IAuthorizationPolicy<Match> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateFormatter;
        private readonly IMatchResultEvaluator _matchResultEvaluator;

        public EditCloseOfPlayController(ILogger<EditCloseOfPlayController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IMatchDataSource matchDataSource,
            IAuthorizationPolicy<Match> authorizationPolicy,
            IDateTimeFormatter dateFormatter,
            IMatchResultEvaluator matchResultEvaluator)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
            _matchResultEvaluator = matchResultEvaluator ?? throw new ArgumentNullException(nameof(matchResultEvaluator));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true)]
        public async new Task<IActionResult> Index()
        {
            var model = new EditCloseOfPlayViewModel(CurrentPage)
            {
                Match = await _matchDataSource.ReadMatchByRoute(Request.Path)
            };

            if (model.Match == null || model.Match.Tournament != null)
            {
                return NotFound();
            }
            else
            {
                // This page is only for matches in the past
                if (model.Match.StartTime > DateTime.UtcNow)
                {
                    return NotFound();
                }

                // This page is not for matches not played
                if (model.Match.MatchResultType.HasValue && new List<MatchResultType> {
                    MatchResultType.HomeWinByForfeit, MatchResultType.AwayWinByForfeit, MatchResultType.Postponed, MatchResultType.Cancelled
                }.Contains(model.Match.MatchResultType.Value))
                {
                    return NotFound();
                }

                model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(model.Match);

                model.FormData.MatchResultType = model.Match.MatchResultType;
                model.FormData.Awards = model.Match.Awards.Select(x => new MatchAwardViewModel
                {
                    MatchAwardId = x.AwardedToId,
                    PlayerSearch = x.PlayerIdentity.PlayerIdentityName,
                    TeamId = model.Match.Teams.First(team => team.Team.TeamId == x.PlayerIdentity.Team.TeamId).Team.TeamId,
                    Reason = x.Reason
                }).ToList();

                if (!model.Match.MatchResultType.HasValue)
                {
                    model.Match.MatchResultType = _matchResultEvaluator.EvaluateMatchResult(model.Match);
                }

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