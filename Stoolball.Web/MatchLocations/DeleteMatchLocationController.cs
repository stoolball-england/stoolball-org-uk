using Stoolball.Umbraco.Data.Matches;
using Stoolball.Umbraco.Data.MatchLocations;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
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

namespace Stoolball.Web.MatchLocations
{
    public class DeleteMatchLocationController : RenderMvcControllerAsync
    {
        private readonly IMatchLocationDataSource _matchLocationDataSource;
        private readonly IMatchListingDataSource _matchDataSource;

        public DeleteMatchLocationController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IMatchLocationDataSource matchLocationDataSource,
           IMatchListingDataSource matchDataSource)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _matchLocationDataSource = matchLocationDataSource ?? throw new System.ArgumentNullException(nameof(matchLocationDataSource));
            _matchDataSource = matchDataSource ?? throw new System.ArgumentNullException(nameof(matchDataSource));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true)]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var model = new DeleteMatchLocationViewModel(contentModel.Content)
            {
                MatchLocation = await _matchLocationDataSource.ReadMatchLocationByRoute(Request.Url.AbsolutePath, true).ConfigureAwait(false),
            };

            if (model.MatchLocation == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.TotalMatches = await _matchDataSource.ReadTotalMatches(new MatchQuery
                {
                    MatchLocationIds = new List<Guid> { model.MatchLocation.MatchLocationId.Value },
                    IncludeTournamentMatches = true
                }).ConfigureAwait(false);
                model.ConfirmDeleteRequest.RequiredText = model.MatchLocation.Name();

                model.IsAuthorized = IsAuthorized(model);

                model.Metadata.PageTitle = "Delete " + model.MatchLocation.NameAndLocalityOrTown();

                return CurrentTemplate(model);
            }
        }


        /// <summary>
        /// Checks whether the currently signed-in member is authorized to delete this match location
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsAuthorized(DeleteMatchLocationViewModel model)
        {
            return Members.IsMemberAuthorized(null, new[] { Groups.Administrators }, null);
        }
    }
}