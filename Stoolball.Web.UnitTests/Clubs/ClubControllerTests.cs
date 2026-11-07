using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Clubs;
using Stoolball.Data.Abstractions;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Clubs;
using Xunit;

namespace Stoolball.Web.UnitTests.Clubs
{
    public class ClubControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IClubDataSource> _clubDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Club>> _authorizationPolicy = new();

        private ClubController CreateController()
        {
            return new ClubController(
                Mock.Of<ILogger<ClubController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _clubDataSource.Object,
                _authorizationPolicy.Object
            )
            {
                ControllerContext = ControllerContext
            };
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
            var club = new Club { ClubName = "Example club" };
            _clubDataSource.Setup(x => x.ReadClubByRoute(It.IsAny<string>())).ReturnsAsync(club);

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = Assert.IsType<ClubViewModel>(((ViewResult)result).Model);
                Assert.Equal(club, model.Club);
            }
        }

        [Fact]
        public async Task Route_matching_club_sets_authorization()
        {
            var club = new Club { ClubName = "Example club" };
            _clubDataSource.Setup(x => x.ReadClubByRoute(It.IsAny<string>())).ReturnsAsync(club);
            _authorizationPolicy.Setup(x => x.IsAuthorized(club)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditClub, true }, { AuthorizedAction.DeleteClub, false } });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = Assert.IsType<ClubViewModel>(((ViewResult)result).Model)!;
                Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditClub]);
                Assert.False(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.DeleteClub]);
            }
        }

        [Fact]
        public async Task Route_matching_club_sets_page_title_to_club_name()
        {
            _clubDataSource.Setup(x => x.ReadClubByRoute(It.IsAny<string>())).ReturnsAsync(new Club { ClubName = "Example club" });
            _authorizationPolicy.Setup(x => x.IsAuthorized(It.IsAny<Club>())).ReturnsAsync(new Dictionary<AuthorizedAction, bool>());

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = Assert.IsType<ClubViewModel>(((ViewResult)result).Model)!;
                Assert.Equal("Example club", model.Metadata.PageTitle);
            }
        }

        [Fact]
        public async Task Route_matching_club_sets_description_listing_teams()
        {
            var club = new Club { ClubName = "Example club" };
            club.Teams.Add(new Team { TeamName = "Example team" });
            _clubDataSource.Setup(x => x.ReadClubByRoute(It.IsAny<string>())).ReturnsAsync(club);
            _authorizationPolicy.Setup(x => x.IsAuthorized(It.IsAny<Club>())).ReturnsAsync(new Dictionary<AuthorizedAction, bool>());

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = Assert.IsType<ClubViewModel>(((ViewResult)result).Model)!;
                Assert.Equal(club.Description(), model.Metadata.Description);
                Assert.Contains("Example team", model.Metadata.Description);
            }
        }

        [Fact]
        public async Task Route_matching_club_adds_teams_breadcrumb()
        {
            _clubDataSource.Setup(x => x.ReadClubByRoute(It.IsAny<string>())).ReturnsAsync(new Club { ClubName = "Example club" });
            _authorizationPolicy.Setup(x => x.IsAuthorized(It.IsAny<Club>())).ReturnsAsync(new Dictionary<AuthorizedAction, bool>());

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = Assert.IsType<ClubViewModel>(((ViewResult)result).Model)!;
                var breadcrumb = model.Breadcrumbs[^1];
                Assert.Equal(Constants.Pages.Teams, breadcrumb.Name);
                Assert.Equal(new Uri(Constants.Pages.TeamsUrl, UriKind.Relative), breadcrumb.Url);
            }
        }
    }
}
