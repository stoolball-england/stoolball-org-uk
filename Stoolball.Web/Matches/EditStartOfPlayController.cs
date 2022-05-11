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
    public class EditStartOfPlayController : RenderController, IRenderControllerAsync
    {
        private readonly IMatchDataSource _matchDataSource;
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly IAuthorizationPolicy<Match> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateFormatter;
        private readonly IEditMatchHelper _editMatchHelper;

        public EditStartOfPlayController(ILogger<EditStartOfPlayController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IMatchDataSource matchDataSource,
            ISeasonDataSource seasonDataSource,
            IAuthorizationPolicy<Match> authorizationPolicy,
            IDateTimeFormatter dateFormatter,
            IEditMatchHelper editMatchHelper)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
            _editMatchHelper = editMatchHelper ?? throw new ArgumentNullException(nameof(editMatchHelper));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true)]
        public async new Task<IActionResult> Index()
        {
            var model = new EditStartOfPlayViewModel(CurrentPage)
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

                model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(model.Match);

                if (model.Match.MatchResultType.HasValue)
                {
                    model.MatchWentAhead = (model.Match.MatchResultType == MatchResultType.HomeWin || model.Match.MatchResultType == MatchResultType.AwayWin || model.Match.MatchResultType == MatchResultType.Tie);
                }

                if (model.Match.MatchType == MatchType.KnockoutMatch && model.Match.Season != null)
                {
                    model.Match.Season = await _seasonDataSource.ReadSeasonByRoute(model.Match.Season.SeasonRoute, true);
                    model.PossibleHomeTeams = _editMatchHelper.PossibleTeamsAsListItems(model.Match.Season?.Teams);
                    model.PossibleAwayTeams = _editMatchHelper.PossibleTeamsAsListItems(model.Match.Season?.Teams);
                }

                model.HomeTeamId = model.Match.Teams.SingleOrDefault(x => x.TeamRole == TeamRole.Home)?.Team.TeamId;
                model.AwayTeamId = model.Match.Teams.SingleOrDefault(x => x.TeamRole == TeamRole.Away)?.Team.TeamId;
                model.MatchLocationId = model.Match.MatchLocation?.MatchLocationId;
                model.MatchLocationName = model.Match.MatchLocation?.NameAndLocalityOrTownIfDifferent();
                model.TossWonBy = model.Match.Teams.FirstOrDefault(x => x.WonToss.HasValue && x.WonToss.Value)?.MatchTeamId.ToString();
                model.BattedFirst = model.Match.InningsOrderIsKnown ? model.Match.MatchInnings.First().BattingTeam.MatchTeamId.ToString() : null;

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