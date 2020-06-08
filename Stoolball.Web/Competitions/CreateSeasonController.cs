using Stoolball.Competitions;
using Stoolball.Umbraco.Data.Competitions;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using System;
using System.Linq;
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
    public class CreateSeasonController : RenderMvcControllerAsync
    {
        private readonly ICompetitionDataSource _competitionDataSource;

        public CreateSeasonController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ICompetitionDataSource competitionDataSource)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _competitionDataSource = competitionDataSource ?? throw new ArgumentNullException(nameof(competitionDataSource));
        }

        [HttpGet]
        [ContentSecurityPolicy(TinyMCE = true, Forms = true)]
        public override async Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var competition = await _competitionDataSource.ReadCompetitionByRoute(Request.Url.AbsolutePath).ConfigureAwait(false);

            if (competition == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                var model = new SeasonViewModel(contentModel.Content)
                {
                    Season = competition.Seasons.FirstOrDefault() ?? new Season()
                };
                var summerSeason = model.Season.StartYear == model.Season.EndYear;
                model.Season.Competition = competition;
                model.Season.StartYear = model.Season.StartYear == default ? DateTime.Today.Year : model.Season.StartYear + 1;
                model.Season.EndYear = summerSeason ? 0 : 1;

                model.IsAuthorized = IsAuthorized(model);

                var the = model.Season.Competition.CompetitionName.StartsWith("THE ", StringComparison.OrdinalIgnoreCase) ? string.Empty : "the ";
                model.Metadata.PageTitle = $"Add a season in {the}{model.Season.Competition.CompetitionName}";

                return CurrentTemplate(model);
            }
        }

        /// <summary>
        /// Checks whether the currently signed-in member is authorized to edit this competition
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsAuthorized(SeasonViewModel model)
        {
            return Members.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.Editors, model?.Season.Competition.MemberGroupName }, null);
        }
    }
}