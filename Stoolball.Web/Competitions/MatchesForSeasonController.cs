﻿using Stoolball.Dates;
using Stoolball.Umbraco.Data.Competitions;
using Stoolball.Umbraco.Data.Matches;
using Stoolball.Web.Matches;
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

namespace Stoolball.Web.Competitions
{
    public class MatchesForSeasonController : RenderMvcControllerAsync
    {
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly IMatchDataSource _matchDataSource;
        private readonly IDateFormatter _dateFormatter;

        public MatchesForSeasonController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ISeasonDataSource seasonDataSource,
           IMatchDataSource matchDataSource,
           IDateFormatter dateFormatter)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
        }

        [HttpGet]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var season = await _seasonDataSource.ReadSeasonByRoute(Request.Url.AbsolutePath, false).ConfigureAwait(false);

            if (season == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                var model = new SeasonViewModel(contentModel.Content)
                {
                    Season = season,
                    Matches = new MatchListingViewModel
                    {
                        Matches = await _matchDataSource.ReadMatchListings(new MatchQuery
                        {
                            SeasonIds = new List<int> { season.SeasonId.Value }
                        }).ConfigureAwait(false),
                        DateFormatter = _dateFormatter
                    },
                };

                model.Metadata.PageTitle = $"Matches for {model.Season.SeasonFullNameAndPlayerType()}";

                return CurrentTemplate(model);
            }
        }
    }
}