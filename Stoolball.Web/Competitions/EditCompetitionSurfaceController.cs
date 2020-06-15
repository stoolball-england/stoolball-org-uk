using Stoolball.Competitions;
using Stoolball.Umbraco.Data.Competitions;
using Stoolball.Web.Security;
using System;
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
        private readonly ICompetitionDataSource _competitionDataSource;
        private readonly ICompetitionRepository _competitionRepository;

        public EditCompetitionSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, ICompetitionDataSource competitionDataSource, ICompetitionRepository competitionRepository)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _competitionDataSource = competitionDataSource;
            _competitionRepository = competitionRepository ?? throw new System.ArgumentNullException(nameof(competitionRepository));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(TinyMCE = true, Forms = true)]
        public async Task<ActionResult> UpdateCompetition([Bind(Prefix = "Competition", Include = "CompetitionName,FromYear,UntilYear,PlayerType,PlayersPerTeam,Overs,Facebook,Twitter,Instagram,YouTube,Website")] Competition competition)
        {
            if (competition is null)
            {
                throw new System.ArgumentNullException(nameof(competition));
            }

            var beforeUpdate = await _competitionDataSource.ReadCompetitionByRoute(Request.RawUrl).ConfigureAwait(false);
            competition.CompetitionId = beforeUpdate.CompetitionId;
            competition.CompetitionRoute = beforeUpdate.CompetitionRoute;

            // get this from the unvalidated form instead of via modelbinding so that HTML can be allowed
            competition.Introduction = Request.Unvalidated.Form["Competition.Introduction"];
            competition.PublicContactDetails = Request.Unvalidated.Form["Competition.PublicContactDetails"];
            competition.PrivateContactDetails = Request.Unvalidated.Form["Competition.PrivateContactDetails"];

            var isAuthorized = Members.IsMemberAuthorized(null, new[] { Groups.Administrators, beforeUpdate.MemberGroupName }, null);

            if (isAuthorized && ModelState.IsValid)
            {
                var currentMember = Members.GetCurrentMember();
                await _competitionRepository.UpdateCompetition(competition, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                // redirect back to the season actions page that led here (ensuring we don't allow off-site redirects), 
                // or the competition actions if that's not available
                if (!string.IsNullOrEmpty(Request.Form["UrlReferrer"]))
                {
                    return Redirect(new Uri(Request.Form["UrlReferrer"]).AbsolutePath);
                }

                return Redirect(competition.CompetitionRoute + "/edit");
            }

            var viewModel = new CompetitionViewModel(CurrentPage)
            {
                Competition = competition,
                IsAuthorized = isAuthorized,
                UrlReferrer = string.IsNullOrEmpty(Request.Form["UrlReferrer"]) ? null : new Uri(Request.Form["UrlReferrer"])
            };
            viewModel.Metadata.PageTitle = $"Edit {competition.CompetitionName}";
            return View("EditCompetition", viewModel);
        }
    }
}