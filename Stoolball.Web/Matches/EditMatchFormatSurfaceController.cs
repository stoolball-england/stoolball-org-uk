using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
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
    public class EditMatchFormatSurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly IMatchDataSource _matchDataSource;
        private readonly IMatchRepository _matchRepository;
        private readonly IAuthorizationPolicy<Match> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateTimeFormatter;
        private readonly IMatchInningsFactory _matchInningsFactory;

        public EditMatchFormatSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberManager memberManager,
            IMatchDataSource matchDataSource, IMatchRepository matchRepository,
            IAuthorizationPolicy<Match> authorizationPolicy, IDateTimeFormatter dateTimeFormatter, IMatchInningsFactory matchInningsFactory)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _matchRepository = matchRepository ?? throw new ArgumentNullException(nameof(matchRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateTimeFormatter = dateTimeFormatter ?? throw new ArgumentNullException(nameof(dateTimeFormatter));
            _matchInningsFactory = matchInningsFactory ?? throw new ArgumentNullException(nameof(matchInningsFactory));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<IActionResult> UpdateMatch([Bind(Prefix = "FormData")] EditMatchFormatFormData postedData)
        {
            if (postedData is null)
            {
                throw new ArgumentNullException(nameof(postedData));
            }

            var beforeUpdate = await _matchDataSource.ReadMatchByRoute(Request.Path);

            if (beforeUpdate == null || beforeUpdate.Tournament != null || beforeUpdate.MatchType == MatchType.TrainingSession)
            {
                return NotFound();
            }

            var model = new EditMatchFormatViewModel(CurrentPage, Services.UserService)
            {
                Match = beforeUpdate,
                FormData = postedData
            };

            var matchIsInTheFuture = model.Match.StartTime > DateTimeOffset.UtcNow;
            if (postedData.MatchInnings < 2 || postedData.MatchInnings % 2 != 0)
            {
                ModelState.AddModelError("FormData.MatchInnings", $"A match cannot have an odd number of innings or less than 2 innings.");
            }
            else if (!matchIsInTheFuture && postedData.MatchInnings < model.Match.MatchInnings.Count)
            {
                ModelState.AddModelError("FormData.MatchInnings", $"You cannot reduce the number of innings after a match has happened.");
            }
            else
            {

                while (matchIsInTheFuture && postedData.MatchInnings < model.Match.MatchInnings.Count)
                {
                    model.Match.MatchInnings.RemoveAt(model.Match.MatchInnings.Count - 1);
                }

                if (postedData.MatchInnings > model.Match.MatchInnings.Count)
                {
                    while (postedData.MatchInnings > model.Match.MatchInnings.Count)
                    {
                        var battingMatchTeamId = model.Match.MatchInnings.Count % 2 == 0 ? model.Match.MatchInnings[0].BattingMatchTeamId : model.Match.MatchInnings[1].BattingMatchTeamId;
                        var bowlingMatchTeamId = model.Match.MatchInnings.Count % 2 == 1 ? model.Match.MatchInnings[0].BattingMatchTeamId : model.Match.MatchInnings[1].BattingMatchTeamId;
                        var addedInnings = _matchInningsFactory.CreateMatchInnings(model.Match, battingMatchTeamId, bowlingMatchTeamId);
                        if (postedData.Overs.HasValue)
                        {
                            addedInnings.OverSets[0].Overs = postedData.Overs;
                        }
                        else
                        {
                            addedInnings.OverSets.Clear();
                        }
                        model.Match.MatchInnings.Add(addedInnings);
                    }
                }

                foreach (var innings in model.Match.MatchInnings)
                {
                    foreach (var overSet in innings.OverSets)
                    {
                        if (!matchIsInTheFuture && postedData.Overs < overSet.Overs)
                        {
                            ModelState.AddModelError("FormData.Overs", $"You cannot reduce the number of overs after a match has happened.");
                        }
                        else
                        {
                            overSet.Overs = postedData.Overs;
                        }
                    }
                }
            }

            model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(beforeUpdate);

            if (model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditMatch] && ModelState.IsValid)
            {
                var currentMember = await _memberManager.GetCurrentMemberAsync();
                var updatedMatch = await _matchRepository.UpdateMatchFormat(model.Match, currentMember.Key, currentMember.Name);

                return Redirect(updatedMatch.MatchRoute + "/edit");
            }

            model.Metadata.PageTitle = "Edit " + model.Match.MatchFullName(x => _dateTimeFormatter.FormatDate(x, false, false, false));

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

            return View("EditMatchFormat", model);
        }
    }
}