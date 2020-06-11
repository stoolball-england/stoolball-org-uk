using Stoolball.Dates;
using Stoolball.Umbraco.Data.Matches;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Web.Matches
{
    public class DeleteMatchController : RenderMvcControllerAsync
    {
        private readonly IMatchDataSource _matchDataSource;
        private readonly IDateTimeFormatter _dateFormatter;

        public DeleteMatchController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IMatchDataSource matchDataSource,
           IDateTimeFormatter dateFormatter)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _matchDataSource = matchDataSource ?? throw new System.ArgumentNullException(nameof(matchDataSource));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true)]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var model = new DeleteMatchViewModel(contentModel.Content)
            {
                Match = await _matchDataSource.ReadMatchByRoute(Request.Url.AbsolutePath).ConfigureAwait(false)
            };

            if (model.Match == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.ConfirmDeleteRequest.RequiredText = model.Match.MatchName;

                model.IsAuthorized = IsAuthorized(model);

                model.Metadata.PageTitle = "Delete " + model.Match.MatchName;

                if (model.Match.Tournament != null)
                {
                    var inThe = (model.Match.Tournament.TournamentName.StartsWith("THE ", StringComparison.OrdinalIgnoreCase)) ? " in " : " in the ";
                    model.Metadata.PageTitle += inThe + model.Match.Tournament.TournamentName;
                }
                model.Metadata.PageTitle += $", {_dateFormatter.FormatDate(model.Match.StartTime.LocalDateTime, false, false, false)} - stoolball match";

                return CurrentTemplate(model);
            }
        }

        /// <summary>
        /// Checks whether the currently signed-in member is authorized to delete this match
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsAuthorized(DeleteMatchViewModel model)
        {
            return Members.IsMemberAuthorized(null, new[] { Groups.Administrators, Groups.Editors }, null);
        }
    }
}