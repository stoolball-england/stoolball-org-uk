using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Metadata;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Stoolball.Web.Competitions
{
    public class DeleteCompetitionController : RenderMvcControllerAsync
    {
        private readonly ICompetitionDataSource _competitionDataSource;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly ITeamDataSource _teamDataSource;
        private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;

        public DeleteCompetitionController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ICompetitionDataSource competitionDataSource,
           IMatchListingDataSource matchDataSource,
           ITeamDataSource teamDataSource,
           IAuthorizationPolicy<Competition> authorizationPolicy)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _competitionDataSource = competitionDataSource ?? throw new ArgumentNullException(nameof(competitionDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true)]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var model = new DeleteCompetitionViewModel(contentModel.Content, Services?.UserService)
            {
                Competition = await _competitionDataSource.ReadCompetitionByRoute(Request.RawUrl).ConfigureAwait(false)
            };

            if (model.Competition == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                var competitionIds = new List<Guid> { model.Competition.CompetitionId.Value };
                model.TotalMatches = await _matchDataSource.ReadTotalMatches(new MatchQuery
                {
                    CompetitionIds = competitionIds,
                    IncludeTournamentMatches = true
                }).ConfigureAwait(false);

                model.TotalTeams = await _teamDataSource.ReadTotalTeams(new TeamQuery
                {
                    CompetitionIds = competitionIds
                }).ConfigureAwait(false);

                model.ConfirmDeleteRequest.RequiredText = model.Competition.CompetitionName;

                model.IsAuthorized = _authorizationPolicy.IsAuthorized(model.Competition);

                model.Metadata.PageTitle = "Delete " + model.Competition.CompetitionName;

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}