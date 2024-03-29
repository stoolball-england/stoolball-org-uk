﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Data.Abstractions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.Navigation;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Stoolball.Web.Teams.Models;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Teams
{
    public class MatchesForTeamController : RenderController, IRenderControllerAsync
    {
        private readonly ITeamDataSource _teamDataSource;
        private readonly IMatchFilterFactory _matchFilterFactory;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IDateTimeFormatter _dateFormatter;
        private readonly ICreateMatchSeasonSelector _createMatchSeasonSelector;
        private readonly IAuthorizationPolicy<Team> _authorizationPolicy;
        private readonly IMatchFilterQueryStringParser _matchFilterQueryStringParser;
        private readonly IMatchFilterHumanizer _matchFilterHumanizer;
        private readonly IAddMatchMenuViewModelFactory _addMatchMenuViewModelFactory;
        private readonly ITeamBreadcrumbBuilder _breadcrumbBuilder;

        public MatchesForTeamController(ILogger<MatchesForTeamController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            ITeamDataSource teamDataSource,
            IMatchFilterFactory matchFilterFactory,
            IMatchListingDataSource matchDataSource,
            IDateTimeFormatter dateFormatter,
            ICreateMatchSeasonSelector createMatchSeasonSelector,
            IAuthorizationPolicy<Team> authorizationPolicy,
            IMatchFilterQueryStringParser matchFilterQueryStringParser,
            IMatchFilterHumanizer matchFilterHumanizer,
            IAddMatchMenuViewModelFactory addMatchMenuViewModelFactory,
            ITeamBreadcrumbBuilder breadcrumbBuilder)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _matchFilterFactory = matchFilterFactory ?? throw new ArgumentNullException(nameof(matchFilterFactory));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
            _createMatchSeasonSelector = createMatchSeasonSelector ?? throw new ArgumentNullException(nameof(createMatchSeasonSelector));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _matchFilterQueryStringParser = matchFilterQueryStringParser ?? throw new ArgumentNullException(nameof(matchFilterQueryStringParser));
            _matchFilterHumanizer = matchFilterHumanizer ?? throw new ArgumentNullException(nameof(matchFilterHumanizer));
            _addMatchMenuViewModelFactory = addMatchMenuViewModelFactory ?? throw new ArgumentNullException(nameof(addMatchMenuViewModelFactory));
            _breadcrumbBuilder = breadcrumbBuilder ?? throw new ArgumentNullException(nameof(breadcrumbBuilder));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var team = await _teamDataSource.ReadTeamByRoute(Request.Path, true);

            if (team == null)
            {
                return NotFound();
            }
            else
            {
                var filter = _matchFilterFactory.MatchesForTeams(new List<Guid> { team.TeamId!.Value });
                var model = new TeamViewModel(CurrentPage)
                {
                    Team = team,
                    DefaultMatchFilter = filter.filter,
                    Matches = new MatchListingViewModel(CurrentPage)
                };

                model.AppliedMatchFilter = _matchFilterQueryStringParser.ParseQueryString(model.DefaultMatchFilter, Request.QueryString.Value);
                model.Matches.Matches = await _matchDataSource.ReadMatchListings(model.AppliedMatchFilter, filter.sortOrder);

                model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(model.Team);
                model.IsInACurrentLeague = _createMatchSeasonSelector.SelectPossibleSeasons(model.Team.Seasons, MatchType.LeagueMatch).Any();
                model.IsInACurrentKnockoutCompetition = _createMatchSeasonSelector.SelectPossibleSeasons(model.Team.Seasons, MatchType.KnockoutMatch).Any();

                model.AddMatchMenu = _addMatchMenuViewModelFactory.CreateModel(model.Team.TeamRoute, false, true, true, model.IsInACurrentKnockoutCompetition, model.IsInACurrentLeague, true);

                var userFilter = _matchFilterHumanizer.MatchingFilter(model.AppliedMatchFilter);
                if (!string.IsNullOrWhiteSpace(userFilter))
                {
                    model.FilterDescription = _matchFilterHumanizer.MatchesAndTournaments(model.AppliedMatchFilter) + userFilter;
                }
                model.Metadata.PageTitle = $"{_matchFilterHumanizer.MatchesAndTournaments(model.AppliedMatchFilter)} for {model.Team.TeamName} stoolball team{userFilter}";

                _breadcrumbBuilder.BuildBreadcrumbsForTeam(model.Breadcrumbs, model.Team, false);

                return CurrentTemplate(model);
            }
        }
    }
}