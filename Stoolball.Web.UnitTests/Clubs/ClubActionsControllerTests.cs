using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Clubs;
using Stoolball.Data.Abstractions;
using Stoolball.Security;
using Stoolball.Web.Clubs;
using Xunit;

namespace Stoolball.Web.UnitTests.Clubs
{
    public class ClubActionsControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IClubDataSource> _clubDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Club>> _authorizationPolicy = new();
        private readonly Club _club = new() { ClubName = "Example club", ClubRoute = "/clubs/example" };

        private ClubActionsController CreateController()
        {
            return new ClubActionsController(
                Mock.Of<ILogger<ClubActionsController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _clubDataSource.Object,
                _authorizationPolicy.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private async Task<ClubViewModel> RunWithMatchingClub()
        {
            _clubDataSource.Setup(x => x.ReadClubByRoute(It.IsAny<string>())).ReturnsAsync(_club);
            _authorizationPolicy.Setup(x => x.IsAuthorized(_club)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditClub, true } });
            _authorizationPolicy.Setup(x => x.AuthorizedMemberNames(_club)).ReturnsAsync(new List<string> { "Jo Bloggs" });
            _authorizationPolicy.Setup(x => x.AuthorizedGroupNames(_club)).Returns(new List<string> { "Administrators" });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                return Assert.IsType<ClubViewModel>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task Route_not_matching_club_returns_404()
        {
            _clubDataSource.Setup(x => x.ReadClubByRoute(It.IsAny<string>())).Returns(Task.FromResult<Club?>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_not_matching_club_does_not_check_authorization()
        {
            _clubDataSource.Setup(x => x.ReadClubByRoute(It.IsAny<string>())).Returns(Task.FromResult<Club?>(null));

            using (var controller = CreateController())
            {
                await controller.Index();

                _authorizationPolicy.Verify(x => x.IsAuthorized(It.IsAny<Club>()), Times.Never);
            }
        }

        [Fact]
        public async Task Route_matching_club_returns_ClubViewModel_containing_club()
        {
            var model = await RunWithMatchingClub();

            Assert.Equal(_club, model.Club);
        }

        [Fact]
        public async Task Route_matching_club_sets_authorization_for_club_name()
        {
            var model = await RunWithMatchingClub();

            Assert.Equal(_club.ClubName, model.Authorization.AuthorizationFor);
        }

        [Fact]
        public async Task Route_matching_club_sets_current_member_authorization()
        {
            var model = await RunWithMatchingClub();

            Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditClub]);
        }

        [Fact]
        public async Task Route_matching_club_sets_authorized_member_and_group_names()
        {
            var model = await RunWithMatchingClub();

            Assert.Equal(new[] { "Jo Bloggs" }, model.Authorization.AuthorizedMemberNames);
            Assert.Equal(new[] { "Administrators" }, model.Authorization.AuthorizedGroupNames);
        }

        [Fact]
        public async Task Route_matching_club_sets_page_title()
        {
            var model = await RunWithMatchingClub();

            Assert.Equal("Edit Example club", model.Metadata.PageTitle);
        }

        [Fact]
        public async Task Route_matching_club_adds_teams_and_club_breadcrumbs()
        {
            var model = await RunWithMatchingClub();

            var teams = model.Breadcrumbs[^2];
            var club = model.Breadcrumbs[^1];
            Assert.Equal(Constants.Pages.Teams, teams.Name);
            Assert.Equal(new Uri(Constants.Pages.TeamsUrl, UriKind.Relative), teams.Url);
            Assert.Equal(_club.ClubName, club.Name);
            Assert.Equal(new Uri(_club.ClubRoute!, UriKind.Relative), club.Url);
        }
    }
}
