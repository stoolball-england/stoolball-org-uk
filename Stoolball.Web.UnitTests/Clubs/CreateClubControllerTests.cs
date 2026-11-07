using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Clubs;
using Stoolball.Security;
using Stoolball.Web.Clubs;
using Xunit;

namespace Stoolball.Web.UnitTests.Clubs
{
    public class CreateClubControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IAuthorizationPolicy<Club>> _authorizationPolicy = new();

        private CreateClubController CreateController()
        {
            return new CreateClubController(
                Mock.Of<ILogger<CreateClubController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _authorizationPolicy.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Returns_ClubViewModel_containing_new_club()
        {
            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = Assert.IsType<ClubViewModel>(((ViewResult)result).Model);
                Assert.NotNull(model.Club);
                Assert.Null(model.Club!.ClubId);
            }
        }

        [Fact]
        public async Task Sets_authorization_using_the_new_club()
        {
            _authorizationPolicy.Setup(x => x.IsAuthorized(It.IsAny<Club>())).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.CreateClub, true } });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = Assert.IsType<ClubViewModel>(((ViewResult)result).Model);
                _authorizationPolicy.Verify(x => x.IsAuthorized(model.Club!), Times.Once);
                Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.CreateClub]);
            }
        }

        [Fact]
        public async Task Sets_page_title()
        {
            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = Assert.IsType<ClubViewModel>(((ViewResult)result).Model);
                Assert.Equal("Add a club", model.Metadata.PageTitle);
            }
        }

        [Fact]
        public async Task Adds_teams_breadcrumb()
        {
            using (var controller = CreateController())
            {
                var result = await controller.Index();

                var model = Assert.IsType<ClubViewModel>(((ViewResult)result).Model);
                var breadcrumb = model.Breadcrumbs[^1];
                Assert.Equal(Constants.Pages.Teams, breadcrumb.Name);
                Assert.Equal(new Uri(Constants.Pages.TeamsUrl, UriKind.Relative), breadcrumb.Url);
            }
        }
    }
}
