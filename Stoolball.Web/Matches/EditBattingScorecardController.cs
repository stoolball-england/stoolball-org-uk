using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Umbraco.Data.Matches;
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
                DateFormatter = _dateFormatter,
                InningsOrderInMatch = _matchInningsUrlParser.ParseInningsOrderInMatchFromUrl(new Uri(Request.RawUrl, UriKind.Relative))
            };

            if (model.Match == null || !model.InningsOrderInMatch.HasValue)
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

                model.CurrentInnings = model.Match.MatchInnings.Single(x => x.InningsOrderInMatch == model.InningsOrderInMatch);
                if (!model.Match.PlayersPerTeam.HasValue)
                {
                    model.Match.PlayersPerTeam = model.Match.Tournament != null ? 8 : 11;
                }
                _playerInningsScaffolder.ScaffoldPlayerInnings(model.CurrentInnings.PlayerInnings, model.Match.PlayersPerTeam.Value);

                model.Metadata.PageTitle = "Edit " + model.Match.MatchFullName(x => _dateFormatter.FormatDate(x.LocalDateTime, false, false, false));

                return CurrentTemplate(model);
            }
        }
    }
}