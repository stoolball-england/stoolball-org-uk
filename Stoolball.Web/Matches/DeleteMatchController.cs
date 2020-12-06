using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Stoolball.Web.Matches
{
    public class DeleteMatchController : RenderMvcControllerAsync
    {
        private readonly IMatchDataSource _matchDataSource;
        private readonly ICommentsDataSource<Match> _matchCommentsDataSource;
        private readonly IAuthorizationPolicy<Match> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateFormatter;

        public DeleteMatchController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IMatchDataSource matchDataSource,
           ICommentsDataSource<Match> matchCommentsDataSource,
           IAuthorizationPolicy<Match> authorizationPolicy,
           IDateTimeFormatter dateFormatter)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _matchDataSource = matchDataSource ?? throw new System.ArgumentNullException(nameof(matchDataSource));
            _matchCommentsDataSource = matchCommentsDataSource ?? throw new ArgumentNullException(nameof(matchCommentsDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
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

            var model = new DeleteMatchViewModel(contentModel.Content, Services?.UserService)
            {
                Match = await _matchDataSource.ReadMatchByRoute(Request.RawUrl).ConfigureAwait(false),
                DateTimeFormatter = _dateFormatter
            };

            if (model.Match == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.TotalComments = await _matchCommentsDataSource.ReadTotalComments(model.Match.MatchId.Value).ConfigureAwait(false);

                model.ConfirmDeleteRequest.RequiredText = model.Match.MatchName;

                model.IsAuthorized = _authorizationPolicy.IsAuthorized(model.Match);

                model.Metadata.PageTitle = "Delete " + model.Match.MatchFullName(x => _dateFormatter.FormatDate(x.LocalDateTime, false, false, false)) + " - stoolball match";

                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Match.MatchName, Url = new Uri(model.Match.MatchRoute, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}