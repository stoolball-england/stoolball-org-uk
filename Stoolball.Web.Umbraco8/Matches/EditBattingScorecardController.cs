using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Navigation;
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
    public class EditBattingScorecardController : RenderMvcControllerAsync
    {
        private readonly IMatchDataSource _matchDataSource;
        private readonly IAuthorizationPolicy<Match> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateFormatter;
        private readonly IMatchInningsUrlParser _matchInningsUrlParser;
        private readonly IPlayerInningsScaffolder _playerInningsScaffolder;

        public EditBattingScorecardController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IMatchDataSource matchDataSource,
           IAuthorizationPolicy<Match> authorizationPolicy,
           IDateTimeFormatter dateFormatter,
           IMatchInningsUrlParser matchInningsUrlParser,
           IPlayerInningsScaffolder playerInningsScaffolder)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
            _matchInningsUrlParser = matchInningsUrlParser ?? throw new ArgumentNullException(nameof(matchInningsUrlParser));
            _playerInningsScaffolder = playerInningsScaffolder ?? throw new ArgumentNullException(nameof(playerInningsScaffolder));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true)]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var model = new EditScorecardViewModel(contentModel.Content, Services?.UserService)
            {
                Match = await _matchDataSource.ReadMatchByRoute(Request.RawUrl).ConfigureAwait(false),
                InningsOrderInMatch = _matchInningsUrlParser.ParseInningsOrderInMatchFromUrl(new Uri(Request.RawUrl, UriKind.Relative)),
                DateFormatter = _dateFormatter,
                Autofocus = true
            };

            if (model.Match == null || !model.InningsOrderInMatch.HasValue || model.Match.Tournament != null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                // This page is only for matches in the past
                if (model.Match.StartTime > DateTime.UtcNow)
                {
                    return new HttpNotFoundResult();
                }

                // This page is not for matches not played
                if (model.Match.MatchResultType.HasValue && new List<MatchResultType> {
                    MatchResultType.HomeWinByForfeit, MatchResultType.AwayWinByForfeit, MatchResultType.Postponed, MatchResultType.Cancelled
                }.Contains(model.Match.MatchResultType.Value))
                {
                    return new HttpNotFoundResult();
                }

                model.IsAuthorized = _authorizationPolicy.IsAuthorized(model.Match);

                model.CurrentInnings.MatchInnings = model.Match.MatchInnings.Single(x => x.InningsOrderInMatch == model.InningsOrderInMatch);
                if (!model.Match.PlayersPerTeam.HasValue)
                {
                    model.Match.PlayersPerTeam = model.Match.Tournament != null ? 8 : 11;
                }
                _playerInningsScaffolder.ScaffoldPlayerInnings(model.CurrentInnings.MatchInnings.PlayerInnings, model.Match.PlayersPerTeam.Value);

                // Convert player innings to a view model, purely to change the field names to ones which will not trigger pop-up contact/password managers 
                // while retaining the benefits of ASP.NET model binding. Using the "search" keyword in the property name also helps to disable contact/password managers.
                model.CurrentInnings.PlayerInningsSearch.AddRange(model.CurrentInnings.MatchInnings.PlayerInnings.Select(x => new PlayerInningsViewModel
                {
                    Batter = x.Batter?.PlayerIdentityName,
                    DismissalType = x.DismissalType,
                    DismissedBy = x.DismissedBy?.PlayerIdentityName,
                    Bowler = x.Bowler?.PlayerIdentityName,
                    RunsScored = x.RunsScored,
                    BallsFaced = x.BallsFaced
                }));

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