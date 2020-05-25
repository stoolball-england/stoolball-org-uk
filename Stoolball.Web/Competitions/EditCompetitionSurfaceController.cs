using Stoolball.Competitions;
using Stoolball.Umbraco.Data.Competitions;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Web.Competitions
{
    public class EditCompetitionSurfaceController : SurfaceController
    {
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly ICompetitionRepository _competitionRepository;

        public EditCompetitionSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, ISeasonDataSource seasonDataSource, ICompetitionRepository competitionRepository)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _seasonDataSource = seasonDataSource;
            _competitionRepository = competitionRepository ?? throw new System.ArgumentNullException(nameof(competitionRepository));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        public async Task<ActionResult> UpdateCompetition([Bind(Prefix = "Competition", Include = "CompetitionName,FromYear,UntilYear,PlayerType,PlayersPerTeam,Overs,Facebook,Twitter,Instagram,YouTube,Website")]Competition competition)
        {
            if (competition is null)
            {
                throw new System.ArgumentNullException(nameof(competition));
            }

            var beforeUpdate = await _seasonDataSource.ReadCompetitionByRoute(Request.RawUrl).ConfigureAwait(false);
            competition.CompetitionId = beforeUpdate.CompetitionId;
            competition.CompetitionRoute = beforeUpdate.CompetitionRoute;

            // get this from the unvalidated form instead of via modelbinding so that HTML can be allowed
            competition.Introduction = Request.Unvalidated.Form["Competition.Introduction"];
            competition.PublicContactDetails = Request.Unvalidated.Form["Competition.PublicContactDetails"];
            competition.PrivateContactDetails = Request.Unvalidated.Form["Competition.PrivateContactDetails"];

            var isAuthorized = Members.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.Editors, beforeUpdate.MemberGroupName }, null);

            if (isAuthorized && ModelState.IsValid)
            {
                var currentMember = Members.GetCurrentMember();
                await _competitionRepository.UpdateCompetition(competition, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                // redirect back to the competition
                return Redirect(competition.CompetitionRoute);
            }

            var viewModel = new CompetitionViewModel(CurrentPage)
            {
                Competition = competition,
                IsAuthorized = isAuthorized
            };
            viewModel.Metadata.PageTitle = $"Edit {competition.CompetitionName}";
            return View("EditCompetition", viewModel);
        }
    }
}