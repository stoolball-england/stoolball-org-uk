using Stoolball.Dates;
using Stoolball.Email;
using Stoolball.Matches;
using Stoolball.Umbraco.Data.Matches;
using Stoolball.Web.Routing;
using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Stoolball.Web.Matches
{
    public class MatchController : RenderMvcControllerAsync
    {
        private readonly IMatchDataSource _matchDataSource;
        private readonly IDateTimeFormatter _dateFormatter;
        private readonly IEmailProtector _emailProtector;

        public MatchController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IMatchDataSource matchDataSource,
           IDateTimeFormatter dateFormatter,
           IEmailProtector emailProtector)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _matchDataSource = matchDataSource ?? throw new System.ArgumentNullException(nameof(matchDataSource));
            _dateFormatter = dateFormatter ?? throw new System.ArgumentNullException(nameof(dateFormatter));
            _emailProtector = emailProtector ?? throw new System.ArgumentNullException(nameof(emailProtector));
        }

        [HttpGet]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var model = new MatchViewModel(contentModel.Content)
            {
                Match = await _matchDataSource.ReadMatchByRoute(Request.Url.AbsolutePath).ConfigureAwait(false),
                DateTimeFormatter = _dateFormatter
            };

            if (model.Match == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.Metadata.PageTitle = model.Match.MatchName;

                if (model.Match.MatchType == MatchType.TournamentMatch)
                {
                    var inThe = (model.Match.Tournament.TournamentName.StartsWith("THE ", StringComparison.OrdinalIgnoreCase)) ? " in " : " in the ";
                    model.Metadata.PageTitle += inThe + model.Match.Tournament.TournamentName;
                }
                model.Metadata.PageTitle += $", {_dateFormatter.FormatDate(model.Match.StartTime.LocalDateTime, false, false, false)} - stoolball match";

                model.Metadata.Description = model.Match.Description();

                return CurrentTemplate(model);
            }
        }
    }
}