using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Website.Controllers;

namespace Stoolball.Web.Matches
{
    public class EditBowlingScorecardSurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly IMatchDataSource _matchDataSource;
        private readonly IMatchRepository _matchRepository;
        private readonly IAuthorizationPolicy<Match> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateTimeFormatter;
        private readonly IMatchInningsUrlParser _matchInningsUrlParser;
        private readonly IBowlingFiguresCalculator _bowlingFiguresCalculator;
        private readonly IOverSetScaffolder _overSetScaffolder;

        public EditBowlingScorecardSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberManager memberManager,
            IMatchDataSource matchDataSource, IMatchRepository matchRepository, IAuthorizationPolicy<Match> authorizationPolicy, IDateTimeFormatter dateTimeFormatter,
            IMatchInningsUrlParser matchInningsUrlParser, IBowlingFiguresCalculator bowlingFiguresCalculator, IOverSetScaffolder overSetScaffolder)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _matchRepository = matchRepository ?? throw new ArgumentNullException(nameof(matchRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateTimeFormatter = dateTimeFormatter ?? throw new ArgumentNullException(nameof(dateTimeFormatter));
            _matchInningsUrlParser = matchInningsUrlParser ?? throw new ArgumentNullException(nameof(matchInningsUrlParser));
            _bowlingFiguresCalculator = bowlingFiguresCalculator ?? throw new ArgumentNullException(nameof(bowlingFiguresCalculator));
            _overSetScaffolder = overSetScaffolder ?? throw new ArgumentNullException(nameof(overSetScaffolder));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<ActionResult> UpdateMatch([Bind("MatchInnings", "OversBowledSearch", Prefix = "CurrentInnings")] MatchInningsViewModel postedData)
        {
            if (postedData is null)
            {
                throw new ArgumentNullException(nameof(postedData));
            }

            var beforeUpdate = await _matchDataSource.ReadMatchByRoute(Request.Path);

            if (beforeUpdate.StartTime > DateTime.UtcNow || beforeUpdate.Tournament != null)
            {
                return NotFound();
            }

            if (beforeUpdate.MatchResultType.HasValue && new List<MatchResultType> {
                    MatchResultType.HomeWinByForfeit, MatchResultType.AwayWinByForfeit, MatchResultType.Postponed, MatchResultType.Cancelled
                }.Contains(beforeUpdate.MatchResultType.Value))
            {
                return NotFound();
            }

            // The bowler name is required if any other fields are filled in for an over
            var i = 0;
            foreach (var over in postedData.OversBowledSearch)
            {
                if (string.IsNullOrWhiteSpace(over.BowledBy) &&
                    (over.BallsBowled.HasValue ||
                     over.Wides.HasValue ||
                     over.NoBalls.HasValue ||
                     over.RunsConceded.HasValue))
                {
                    ModelState.AddModelError($"CurrentInnings.OversBowledSearch[{i}].Bowler", $"You've added the {(i + 1).Ordinalize(CultureInfo.CurrentCulture)} over. Please name the bowler.");
                }
                i++;
            }

            var model = new EditScorecardViewModel(CurrentPage, Services.UserService)
            {
                Match = beforeUpdate,
                InningsOrderInMatch = _matchInningsUrlParser.ParseInningsOrderInMatchFromUrl(new Uri(Request.Path, UriKind.Relative)),
                Autofocus = true
            };
            model.CurrentInnings.MatchInnings = model.Match.MatchInnings.Single(x => x.InningsOrderInMatch == model.InningsOrderInMatch);
            model.CurrentInnings.MatchInnings.OversBowled = postedData.OversBowledSearch.Where(x => x.BowledBy?.Trim().Length > 0).Select((x, index) => new Over
            {
                Bowler = new PlayerIdentity
                {
                    PlayerIdentityName = x.BowledBy?.Trim(),
                    Team = model.CurrentInnings.MatchInnings.BowlingTeam.Team
                },
                OverNumber = index + 1,
                BallsBowled = x.BallsBowled,
                Wides = x.Wides,
                NoBalls = x.NoBalls,
                RunsConceded = x.RunsConceded
            }).ToList();

            model.CurrentInnings.OversBowledSearch = postedData.OversBowledSearch;

            _overSetScaffolder.ScaffoldOverSets(model.CurrentInnings.MatchInnings.OverSets, model.Match.Tournament != null ? 6 : 12, model.CurrentInnings.MatchInnings.OversBowled);

            model.CurrentInnings.MatchInnings.BowlingFigures = _bowlingFiguresCalculator.CalculateBowlingFigures(model.CurrentInnings.MatchInnings);

            model.IsAuthorized = await _authorizationPolicy.IsAuthorized(beforeUpdate);

            if (model.IsAuthorized[AuthorizedAction.EditMatchResult] && ModelState.IsValid)
            {
                var currentMember = await _memberManager.GetCurrentMemberAsync();
                await _matchRepository.UpdateBowlingScorecard(model.Match, model.CurrentInnings.MatchInnings.MatchInningsId!.Value, currentMember.Key, currentMember.Name);

                // redirect to the next innings or close of play
                if (model.InningsOrderInMatch!.Value < model.Match.MatchInnings.Count)
                {
                    return Redirect($"{model.Match.MatchRoute}/edit/innings/{model.InningsOrderInMatch.Value + 1}/batting");
                }
                else
                {
                    return Redirect(model.Match.MatchRoute + "/edit/close-of-play");
                }
            }

            model.Metadata.PageTitle = "Edit " + model.Match.MatchFullName(x => _dateTimeFormatter.FormatDate(x, false, false, false));

            while (model.CurrentInnings.MatchInnings.OversBowled.Count < model.CurrentInnings.MatchInnings.OverSets.Sum(x => x.Overs))
            {
                model.CurrentInnings.MatchInnings.OversBowled.Add(new Over());
            }

            if (model.Match.Season != null)
            {
                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Match.Season.Competition.CompetitionName, Url = new Uri(model.Match.Season.Competition.CompetitionRoute, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Match.Season.SeasonName(), Url = new Uri(model.Match.Season.SeasonRoute, UriKind.Relative) });
            }
            else
            {
                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Matches, Url = new Uri(Constants.Pages.MatchesUrl, UriKind.Relative) });
            }
            model.Breadcrumbs.Add(new Breadcrumb { Name = model.Match.MatchName, Url = new Uri(model.Match.MatchRoute, UriKind.Relative) });

            return View("EditBowlingScorecard", model);
        }
    }
}