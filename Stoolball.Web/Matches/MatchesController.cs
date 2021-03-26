using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Dates;
using Stoolball.Matches;
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
    public class MatchesController : RenderMvcControllerAsync
    {
        private readonly IMatchListingDataSource _matchesDataSource;
        private readonly IDateTimeFormatter _dateTimeFormatter;

        public MatchesController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IMatchListingDataSource matchesDataSource,
           IDateTimeFormatter dateTimeFormatter)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _matchesDataSource = matchesDataSource ?? throw new ArgumentNullException(nameof(matchesDataSource));
            _dateTimeFormatter = dateTimeFormatter ?? throw new ArgumentNullException(nameof(dateTimeFormatter));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            _ = int.TryParse(Request.QueryString["page"], out var pageNumber);
            var model = new MatchListingViewModel(contentModel.Content, Services?.UserService)
            {
                MatchFilter = new MatchFilter
                {
                    Query = Request.QueryString["q"]?.Trim(),
                    FromDate = DateTimeOffset.UtcNow.Date
                },
                DateTimeFormatter = _dateTimeFormatter
            };

            model.MatchFilter.Paging.PageNumber = pageNumber > 0 ? pageNumber : 1;
            model.MatchFilter.Paging.PageSize = Constants.Defaults.PageSize;
            model.MatchFilter.Paging.PageUrl = Request.Url;
            model.MatchFilter.Paging.Total = await _matchesDataSource.ReadTotalMatches(model.MatchFilter).ConfigureAwait(false);
            model.Matches = await _matchesDataSource.ReadMatchListings(model.MatchFilter, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false);

            model.Metadata.PageTitle = Constants.Pages.Matches;
            if (!string.IsNullOrEmpty(model.MatchFilter.Query))
            {
                model.Metadata.PageTitle += $" matching '{model.MatchFilter.Query}'";
            }

            return CurrentTemplate(model);
        }
    }
}