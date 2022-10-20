using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Web.Security;
using Stoolball.Web.Statistics.Models;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Statistics
{
    public abstract class BaseStatisticsTableController<T> : RenderController
    {
        private readonly IStatisticsFilterFactory _statisticsFilterFactory;
        private readonly IStatisticsBreadcrumbBuilder _statisticsBreadcrumbBuilder;
        private readonly IStatisticsFilterQueryStringParser _statisticsFilterQueryStringParser;
        private readonly IStatisticsFilterHumanizer _statisticsFilterHumanizer;
        private readonly Func<StatisticsFilter, Task<IEnumerable<StatisticsResult<T>>>> _readResults;
        private readonly Func<StatisticsFilter, Task<int>> _readTotalResults;
        private readonly Func<StatisticsFilter, string> _pageTitle;
        private readonly string _filterEntityPlural;
        private readonly int? _minimumQualifyingInningsUnfiltered;
        private readonly int? _minimumQualifyingInningsFiltered;
        private readonly Func<StatisticsFilter, bool>? _validateFilter;

        protected BaseStatisticsTableController(ILogger<BaseStatisticsTableController<T>> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IStatisticsFilterFactory statisticsFilterFactory,
            IStatisticsBreadcrumbBuilder statisticsBreadcrumbBuilder,
            IStatisticsFilterQueryStringParser statisticsFilterQueryStringParser,
            IStatisticsFilterHumanizer statisticsFilterHumanizer,
            Func<StatisticsFilter, Task<IEnumerable<StatisticsResult<T>>>> readResults,
            Func<StatisticsFilter, Task<int>> readTotalResults,
            Func<StatisticsFilter, string> pageTitle,
            string filterEntityPlural,
            int? minimumQualifyingInningsUnfiltered = null,
            int? minimumQualifyingInningsFiltered = null,
            Func<StatisticsFilter, bool>? validateFilter = null
            )
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            if (string.IsNullOrEmpty(filterEntityPlural))
            {
                throw new ArgumentException($"'{nameof(filterEntityPlural)}' cannot be null or empty.", nameof(filterEntityPlural));
            }

            _statisticsFilterFactory = statisticsFilterFactory ?? throw new ArgumentNullException(nameof(statisticsFilterFactory));
            _statisticsBreadcrumbBuilder = statisticsBreadcrumbBuilder ?? throw new ArgumentNullException(nameof(statisticsBreadcrumbBuilder));
            _statisticsFilterQueryStringParser = statisticsFilterQueryStringParser ?? throw new ArgumentNullException(nameof(statisticsFilterQueryStringParser));
            _statisticsFilterHumanizer = statisticsFilterHumanizer ?? throw new ArgumentNullException(nameof(statisticsFilterHumanizer));
            _readResults = readResults ?? throw new ArgumentNullException(nameof(readResults));
            _readTotalResults = readTotalResults ?? throw new ArgumentNullException(nameof(readTotalResults));
            _pageTitle = pageTitle ?? throw new ArgumentNullException(nameof(pageTitle));
            _filterEntityPlural = filterEntityPlural;
            _minimumQualifyingInningsUnfiltered = minimumQualifyingInningsUnfiltered;
            _minimumQualifyingInningsFiltered = minimumQualifyingInningsFiltered;
            _validateFilter = validateFilter;
        }


        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var model = new StatisticsViewModel<T>(CurrentPage) { ShowCaption = false };
            model.DefaultFilter = await _statisticsFilterFactory.FromRoute(Request.Path);
            model.AppliedFilter = _statisticsFilterQueryStringParser.ParseQueryString(model.DefaultFilter, Request.QueryString.Value);
            if (model.AppliedFilter.Team != null)
            {
                model.ShowTeamsColumn = false;

                if (model.DefaultFilter.Player != null)
                {
                    var teamWithName = model.DefaultFilter.Player.PlayerIdentities.First(x => x.Team != null && x.Team.TeamId == model.AppliedFilter.Team.TeamId).Team;
                    if (teamWithName != null)
                    {
                        model.AppliedFilter.Team = teamWithName;
                    }
                }
            }
            model.AppliedFilter.MinimumQualifyingInnings = _minimumQualifyingInningsUnfiltered;
            if (_minimumQualifyingInningsFiltered.HasValue &&
                (model.AppliedFilter.Team != null ||
                 model.AppliedFilter.Club != null ||
                 model.AppliedFilter.Competition != null ||
                 model.AppliedFilter.Season != null ||
                 model.AppliedFilter.MatchLocation != null)) { model.AppliedFilter.MinimumQualifyingInnings = _minimumQualifyingInningsFiltered; }

            if (_validateFilter != null && !_validateFilter(model.AppliedFilter))
            {
                return NotFound();
            }

            model.Results = (await _readResults(model.AppliedFilter)).ToList();

            model.AppliedFilter.Paging.PageUrl = new Uri(Request.GetEncodedUrl());
            model.AppliedFilter.Paging.Total = await _readTotalResults(model.AppliedFilter);

            _statisticsBreadcrumbBuilder.BuildBreadcrumbs(model.Breadcrumbs, model.AppliedFilter);

            var userFilter = _statisticsFilterHumanizer.MatchingUserFilter(model.AppliedFilter);
            model.FilterViewModel.FilteredItemTypePlural = _filterEntityPlural;
            model.FilterViewModel.FilterDescription = _statisticsFilterHumanizer.EntitiesMatchingFilter(_filterEntityPlural, userFilter);
            model.FilterViewModel.from = model.AppliedFilter.FromDate;
            model.FilterViewModel.to = model.AppliedFilter.UntilDate;
            model.FilterViewModel.team = model.AppliedFilter.Team?.TeamId;
            if (model.DefaultFilter.Player != null)
            {
                model.FilterViewModel.Teams = model.DefaultFilter.Player.PlayerIdentities.OrderByDescending(x => x.TotalMatches).Select(x => x.Team).OfType<Team>().ToList().Distinct(new TeamEqualityComparer());
            }

            model.Metadata.PageTitle = _pageTitle(model.AppliedFilter) + _statisticsFilterHumanizer.MatchingFixedFilter(model.DefaultFilter) + userFilter;
            model.Heading = _pageTitle(model.AppliedFilter) + _statisticsFilterHumanizer.MatchingFixedFilter(model.DefaultFilter);

            return CurrentTemplate(model);
        }
    }
}