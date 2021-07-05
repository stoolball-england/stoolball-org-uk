using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Matches;
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

namespace Stoolball.Web.Matches
{
    public class EditLeagueMatchController : RenderMvcControllerAsync
    {
        private readonly IMatchDataSource _matchDataSource;
        private readonly IAuthorizationPolicy<Match> _authorizationPolicy;
        private readonly IAuthorizationPolicy<Competition> _competitionAuthorizationPolicy;
        private readonly IDateTimeFormatter _dateFormatter;
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly IEditMatchHelper _editMatchHelper;

        public EditLeagueMatchController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IMatchDataSource matchDataSource,
           IAuthorizationPolicy<Match> matchAuthorizationPolicy,
           IAuthorizationPolicy<Competition> competitionAuthorizationPolicy,
           IDateTimeFormatter dateFormatter,
           ISeasonDataSource seasonDataSource,
           IEditMatchHelper editMatchHelper)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _authorizationPolicy = matchAuthorizationPolicy ?? throw new ArgumentNullException(nameof(matchAuthorizationPolicy));
            _competitionAuthorizationPolicy = competitionAuthorizationPolicy ?? throw new ArgumentNullException(nameof(competitionAuthorizationPolicy));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _editMatchHelper = editMatchHelper ?? throw new ArgumentNullException(nameof(editMatchHelper));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true, TinyMCE = true)]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var model = new EditLeagueMatchViewModel(contentModel.Content, Services?.UserService)
            {
                Match = await _matchDataSource.ReadMatchByRoute(Request.RawUrl).ConfigureAwait(false),
                DateFormatter = _dateFormatter
            };

            if (model.Match == null || model.Match.Tournament != null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                // This page is only for matches in the future
                if (model.Match.StartTime <= DateTime.UtcNow)
                {
                    return new HttpNotFoundResult();
                }

                model.IsAuthorized = _authorizationPolicy.IsAuthorized(model.Match);
                if (model.Match.Season != null && model.Match.Season.Teams.Count <= 1 && model.Match.Season.Competition != null)
                {
                    _competitionAuthorizationPolicy.IsAuthorized(model.Match.Season.Competition).TryGetValue(AuthorizedAction.EditCompetition, out var canEditCompetition);
                    model.IsAuthorized[AuthorizedAction.EditCompetition] = canEditCompetition;
                }

                if (model.Match.Season != null)
                {
                    model.Match.Season = model.Season = await _seasonDataSource.ReadSeasonByRoute(model.Match.Season.SeasonRoute, true).ConfigureAwait(false);
                    model.PossibleSeasons = _editMatchHelper.PossibleSeasonsAsListItems(new[] { model.Match.Season });
                }
                model.PossibleHomeTeams = _editMatchHelper.PossibleTeamsAsListItems(model.Season?.Teams);
                model.PossibleAwayTeams = _editMatchHelper.PossibleTeamsAsListItems(model.Season?.Teams);

                model.MatchDate = model.Match.StartTime;
                if (model.Match.StartTimeIsKnown)
                {
                    model.StartTime = model.Match.StartTime;
                }
                model.HomeTeamId = model.Match.Teams.SingleOrDefault(x => x.TeamRole == TeamRole.Home)?.Team.TeamId;
                model.AwayTeamId = model.Match.Teams.SingleOrDefault(x => x.TeamRole == TeamRole.Away)?.Team.TeamId;
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