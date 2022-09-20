using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Caching;
using Stoolball.Competitions;
using Stoolball.Navigation;
using Stoolball.Routing;
using Stoolball.Security;
using Stoolball.Web.Competitions.Models;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Website.Controllers;
using static Stoolball.Constants;

namespace Stoolball.Web.Competitions
{
    public class CreateCompetitionSurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly ICompetitionRepository _competitionRepository;
        private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;
        private readonly IRouteGenerator _routeGenerator;
        private readonly IListingCacheClearer<Competition> _cacheClearer;

        public CreateCompetitionSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberManager memberManager, ICompetitionRepository seasonRepository,
            IAuthorizationPolicy<Competition> authorizationPolicy, IRouteGenerator routeGenerator, IListingCacheClearer<Competition> cacheClearer)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _competitionRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
            _cacheClearer = cacheClearer ?? throw new ArgumentNullException(nameof(cacheClearer));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(TinyMCE = true, Forms = true)]
        public async Task<ActionResult> CreateCompetition([Bind("CompetitionName", "UntilYear", "PlayerType", "Facebook", "Twitter", "Instagram", "YouTube", "Website", Prefix = "Competition")] Competition competition)
        {
            if (competition is null)
            {
                throw new ArgumentNullException(nameof(competition));
            }

            // get this from the form instead of via modelbinding so that HTML can be allowed
            competition.Introduction = Request.Form["Competition.Introduction"];
            competition.PublicContactDetails = Request.Form["Competition.PublicContactDetails"];
            competition.PrivateContactDetails = Request.Form["Competition.PrivateContactDetails"];

            var isAuthorized = await _authorizationPolicy.IsAuthorized(competition);

            if (isAuthorized[AuthorizedAction.CreateCompetition] && ModelState.IsValid)
            {
                // Create an owner group
                var groupName = _routeGenerator.GenerateRoute("competition", competition.CompetitionName, NoiseWords.CompetitionRoute);
                IMemberGroup group;
                do
                {
                    group = Services.MemberGroupService.GetByName(groupName);
                    if (group == null)
                    {
                        group = new MemberGroup
                        {
                            Name = groupName
                        };
                        Services.MemberGroupService.Save(group);
                        competition.MemberGroupKey = group.Key;
                        competition.MemberGroupName = group.Name;
                        break;
                    }
                    else
                    {
                        groupName = _routeGenerator.IncrementRoute(groupName);
                    }
                }
                while (group != null);

                // Assign the current member to the group unless they're already admin
                var currentMember = await _memberManager.GetCurrentMemberAsync();
                if (!await _memberManager.IsMemberAuthorizedAsync(null, new[] { Groups.Administrators }, null))
                {
                    Services.MemberService.AssignRole(currentMember.Id, group!.Name);
                }

                // Create the competition
                var createdCompetition = await _competitionRepository.CreateCompetition(competition, currentMember.Key, currentMember.Name).ConfigureAwait(false);
                _cacheClearer.ClearCache();

                // Redirect to the competition
                return Redirect(createdCompetition.CompetitionRoute);
            }

            var viewModel = new CompetitionViewModel(CurrentPage, Services.UserService)
            {
                Competition = competition,
            };
            viewModel.Authorization.CurrentMemberIsAuthorized = isAuthorized;
            viewModel.Metadata.PageTitle = $"Add a competition";

            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });

            return View("CreateCompetition", viewModel);
        }
    }
}