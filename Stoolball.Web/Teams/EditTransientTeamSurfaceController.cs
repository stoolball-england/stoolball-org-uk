﻿using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Teams;
using Stoolball.Umbraco.Data.Matches;
using Stoolball.Umbraco.Data.Teams;
using Stoolball.Web.Matches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Web.Teams
{
    public class EditTransientTeamSurfaceController : SurfaceController
    {
        private readonly ITeamDataSource _teamDataSource;
        private readonly ITeamRepository _teamRepository;
        private readonly IMatchDataSource _matchDataSource;
        private readonly IDateTimeFormatter _dateFormatter;

        public EditTransientTeamSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, ITeamDataSource teamDataSource, ITeamRepository teamRepository,
            IMatchDataSource matchDataSource, IDateTimeFormatter dateFormatter)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _teamDataSource = teamDataSource;
            _teamRepository = teamRepository ?? throw new System.ArgumentNullException(nameof(teamRepository));
            _matchDataSource = matchDataSource ?? throw new System.ArgumentNullException(nameof(matchDataSource));
            _dateFormatter = dateFormatter ?? throw new System.ArgumentNullException(nameof(dateFormatter));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        public async Task<ActionResult> UpdateTransientTeam([Bind(Prefix = "Team", Include = "TeamName,AgeRangeLower,AgeRangeUpper,PlayerType,Facebook,Twitter,Instagram,YouTube,Website")]Team team)
        {
            if (team is null)
            {
                throw new System.ArgumentNullException(nameof(team));
            }

            var beforeUpdate = await _teamDataSource.ReadTeamByRoute(Request.RawUrl).ConfigureAwait(false);
            team.TeamId = beforeUpdate.TeamId;
            team.TeamRoute = beforeUpdate.TeamRoute;

            // get this from the unvalidated form instead of via modelbinding so that HTML can be allowed
            team.Introduction = Request.Unvalidated.Form["Team.Introduction"];
            team.Cost = Request.Unvalidated.Form["Team.Cost"];
            team.PublicContactDetails = Request.Unvalidated.Form["Team.PublicContactDetails"];
            team.PrivateContactDetails = Request.Unvalidated.Form["Team.PrivateContactDetails"];

            var isAuthorized = Members.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.Editors, beforeUpdate.MemberGroupName }, null);

            if (isAuthorized && ModelState.IsValid)
            {
                var currentMember = Members.GetCurrentMember();
                await _teamRepository.UpdateTransientTeam(team, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                // redirect back to the team
                return Redirect(team.TeamRoute);
            }

            var viewModel = new TeamViewModel(CurrentPage)
            {
                Team = team,
                IsAuthorized = isAuthorized
            };

            viewModel.Matches = new MatchListingViewModel
            {
                Matches = await _matchDataSource.ReadMatchListings(new MatchQuery
                {
                    TeamIds = new List<Guid> { viewModel.Team.TeamId.Value },
                    MatchTypes = new List<MatchType> { MatchType.Tournament }
                }).ConfigureAwait(false),
                DateTimeFormatter = _dateFormatter
            };

            viewModel.Metadata.PageTitle = $"Edit {viewModel.Team.TeamName}, {_dateFormatter.FormatDate(viewModel.Matches.Matches.First().StartTime, false, false)}";
            return View("EditTransientTeam", viewModel);
        }
    }
}