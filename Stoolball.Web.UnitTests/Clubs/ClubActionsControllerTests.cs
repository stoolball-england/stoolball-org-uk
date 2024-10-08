﻿using System.Threading.Tasks;
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

        private ClubActionsController CreateController()
        {
            return new ClubActionsController(
                Mock.Of<ILogger<ClubActionsController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _clubDataSource.Object,
                Mock.Of<IAuthorizationPolicy<Club>>())
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
        public async Task Route_matching_club_returns_ClubViewModel()
        {
            _clubDataSource.Setup(x => x.ReadClubByRoute(It.IsAny<string>())).ReturnsAsync(new Club { ClubRoute = "/clubs/example" });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<ClubViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
