using Stoolball.Umbraco.Data.Competitions;
using Stoolball.Umbraco.Data.Matches;
using Stoolball.Umbraco.Data.Teams;
using Stoolball.Web.Routing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Web.Competitions
{
    public class DeleteCompetitionController : RenderMvcControllerAsync
    {
        private readonly ICompetitionDataSource _competitionDataSource;
        private readonly IMatchDataSource _matchDataSource;
        private readonly ITeamDataSource _teamDataSource;

        public DeleteCompetitionController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ICompetitionDataSource competitionDataSource,
           IMatchDataSource matchDataSource,
           ITeamDataSource teamDataSource)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _competitionDataSource = competitionDataSource ?? throw new System.ArgumentNullException(nameof(competitionDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
        }

        [HttpGet]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var model = new DeleteCompetitionViewModel(contentModel.Content)
            {
                Competition = await _competitionDataSource.ReadCompetitionByRoute(Request.Url.AbsolutePath).ConfigureAwait(false)
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
                    CompetitionIds = competitionIds
                }).ConfigureAwait(false);

                model.TotalTeams = await _teamDataSource.ReadTotalTeams(new TeamQuery
                {
                    CompetitionIds = competitionIds
                }).ConfigureAwait(false);

                model.ConfirmDeleteRequest.RequiredText = model.Competition.CompetitionName;

                model.IsAuthorized = IsAuthorized(model);

                model.Metadata.PageTitle = "Delete " + model.Competition.CompetitionName;

                return CurrentTemplate(model);
            }
        }

        /// <summary>
        /// Checks whether the currently signed-in member is authorized to delete this competition
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsAuthorized(DeleteCompetitionViewModel model)
        {
            return Members.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.Editors }, null);
        }
    }
}