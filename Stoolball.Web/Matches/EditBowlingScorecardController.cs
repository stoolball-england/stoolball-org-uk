﻿using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Umbraco.Data.Matches;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using System;
using System.Collections.Generic;
using System.Linq;
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
    public class EditBowlingScorecardController : RenderMvcControllerAsync
    {
        private readonly IMatchDataSource _matchDataSource;
        private readonly IAuthorizationPolicy<Match> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateFormatter;
        private readonly IMatchInningsUrlParser _matchInningsUrlParser;

        public EditBowlingScorecardController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IMatchDataSource matchDataSource,
           IAuthorizationPolicy<Match> authorizationPolicy,
           IDateTimeFormatter dateFormatter,
           IMatchInningsUrlParser matchInningsUrlParser)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
            _matchInningsUrlParser = matchInningsUrlParser ?? throw new ArgumentNullException(nameof(matchInningsUrlParser));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true)]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var model = new EditBowlingScorecardViewModel(contentModel.Content)
            {
                Match = await _matchDataSource.ReadMatchByRoute(Request.RawUrl).ConfigureAwait(false),
                DateFormatter = _dateFormatter,
                InningsOrderInMatch = _matchInningsUrlParser.ParseInningsOrderInMatchFromUrl(new Uri(Request.RawUrl, UriKind.Relative))
            };

            if (model.Match == null || !model.InningsOrderInMatch.HasValue)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                // This page is only for matches in the past
                if (model.Match.StartTime > DateTime.UtcNow)
                {
                    return new HttpNotFoundResult();
                }

                // This page is not for matches not played
                if (model.Match.MatchResultType.HasValue && new List<MatchResultType> {
                    MatchResultType.HomeWinByForfeit, MatchResultType.AwayWinByForfeit, MatchResultType.Postponed, MatchResultType.Cancelled
                }.Contains(model.Match.MatchResultType.Value))
                {
                    return new HttpNotFoundResult();
                }

                model.IsAuthorized = _authorizationPolicy.IsAuthorized(model.Match, Members);

                model.CurrentInnings = model.Match.MatchInnings.Single(x => x.InningsOrderInMatch == model.InningsOrderInMatch);
                if (!model.CurrentInnings.Overs.HasValue)
                {
                    model.CurrentInnings.Overs = model.Match.Tournament != null ? 6 : 12;
                }
                while (model.CurrentInnings.OversBowled.Count < model.CurrentInnings.Overs)
                {
                    model.CurrentInnings.OversBowled.Add(new Over());
                }

                model.Metadata.PageTitle = "Edit " + model.Match.MatchFullName(x => _dateFormatter.FormatDate(x.LocalDateTime, false, false, false));

                return CurrentTemplate(model);
            }
        }
    }
}