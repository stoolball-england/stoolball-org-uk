using Stoolball.Umbraco.Data.Competitions;
using Stoolball.Umbraco.Data.Matches;
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
    public class DeleteSeasonController : RenderMvcControllerAsync
    {
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly IMatchDataSource _matchDataSource;

        public DeleteSeasonController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ISeasonDataSource seasonDataSource,
           IMatchDataSource matchDataSource)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _seasonDataSource = seasonDataSource ?? throw new System.ArgumentNullException(nameof(seasonDataSource));
            _matchDataSource = matchDataSource ?? throw new System.ArgumentNullException(nameof(matchDataSource));
        }

        [HttpGet]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var model = new DeleteSeasonViewModel(contentModel.Content)
            {
                Season = await _seasonDataSource.ReadSeasonByRoute(Request.Url.AbsolutePath, true).ConfigureAwait(false)
            };

            if (model.Season == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.TotalMatches = await _matchDataSource.ReadTotalMatches(new MatchQuery
                {
                    SeasonIds = new List<Guid> { model.Season.SeasonId.Value }
                }).ConfigureAwait(false);

                model.ConfirmDeleteRequest.RequiredText = model.Season.SeasonFullName();

                model.IsAuthorized = IsAuthorized(model);

                model.Metadata.PageTitle = "Delete " + model.Season.SeasonFullNameAndPlayerType();

                return CurrentTemplate(model);
            }
        }

        /// <summary>
        /// Checks whether the currently signed-in member is authorized to delete this competition
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsAuthorized(DeleteSeasonViewModel model)
        {
            return Members.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.Editors, model?.Season.Competition.MemberGroupName }, null);
        }
    }
}