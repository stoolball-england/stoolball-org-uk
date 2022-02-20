using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Competitions.Models;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Competitions
{
    public class DeleteCompetitionController : RenderController, IRenderControllerAsync
    {
        private readonly ICompetitionDataSource _competitionDataSource;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly ITeamDataSource _teamDataSource;
        private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;

        public DeleteCompetitionController(ILogger<DeleteCompetitionController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            ICompetitionDataSource competitionDataSource,
            IMatchListingDataSource matchDataSource,
            ITeamDataSource teamDataSource,
            IAuthorizationPolicy<Competition> authorizationPolicy)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _competitionDataSource = competitionDataSource ?? throw new ArgumentNullException(nameof(competitionDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true)]
        public async new Task<IActionResult> Index()
        {
            var model = new DeleteCompetitionViewModel(CurrentPage)
            {
                Competition = await _competitionDataSource.ReadCompetitionByRoute(Request.Path).ConfigureAwait(false)
            };

            if (model.Competition == null)
            {
                return NotFound();
            }
            else
            {
                var competitionIds = new List<Guid> { model.Competition.CompetitionId!.Value };
                model.TotalMatches = await _matchDataSource.ReadTotalMatches(new MatchFilter
                {
                    CompetitionIds = competitionIds,
                    IncludeTournamentMatches = true
                }).ConfigureAwait(false);

                model.TotalTeams = await _teamDataSource.ReadTotalTeams(new TeamFilter
                {
                    CompetitionIds = competitionIds
                }).ConfigureAwait(false);

                model.ConfirmDeleteRequest.RequiredText = model.Competition.CompetitionName;

                model.IsAuthorized = await _authorizationPolicy.IsAuthorized(model.Competition);

                model.Metadata.PageTitle = "Delete " + model.Competition.CompetitionName;

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Competition.CompetitionName, Url = new Uri(model.Competition.CompetitionRoute, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}