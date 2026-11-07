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
    public class DeleteClubControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IClubDataSource> _clubDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Club>> _authorizationPolicy = new();
        private readonly Club _club = new() { ClubName = "Example club", ClubRoute = "/clubs/example" };

        private DeleteClubController CreateController()
        {
            return new DeleteClubController(
                Mock.Of<ILogger<DeleteClubController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _clubDataSource.Object,
                _authorizationPolicy.Object
                )
            {
                ControllerContext = ControllerContext
            };
        }

        private async Task<DeleteClubViewModel> RunWithMatchingClub()
        {
            _clubDataSource.Setup(x => x.ReadClubByRoute(It.IsAny<string>())).ReturnsAsync(_club);
            _authorizationPolicy.Setup(x => x.IsAuthorized(_club)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.DeleteClub, true } });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                return Assert.IsType<DeleteClubViewModel>(((ViewResult)result).Model);
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
        public async Task Route_matching_club_returns_DeleteClubViewModel_containing_club()
        {
            var model = await RunWithMatchingClub();

            Assert.Equal(_club, model.Club);
            Assert.False(model.Deleted);
        }

        [Fact]
        public async Task Route_matching_club_requires_club_name_as_confirmation_text()
        {
            var model = await RunWithMatchingClub();

            Assert.Equal(_club.ClubName, model.ConfirmDeleteRequest.RequiredText);
        }

        [Fact]
        public async Task Route_matching_club_sets_authorization()
        {
            var model = await RunWithMatchingClub();

            Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.DeleteClub]);
        }

        [Fact]
        public async Task Route_matching_club_sets_page_title()
        {
            var model = await RunWithMatchingClub();

            Assert.Equal("Delete Example club", model.Metadata.PageTitle);
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
