using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Email;
using Stoolball.Security;
using Stoolball.Web.Competitions;
using Stoolball.Web.Competitions.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Competitions
{
    public class CompetitionControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ICompetitionDataSource> _competitionDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Competition>> _authorizationPolicy = new();
        private readonly Mock<IEmailProtector> _emailProtector = new();

        private CompetitionController CreateController()
        {
            return new CompetitionController(
              Mock.Of<ILogger<CompetitionController>>(),
              CompositeViewEngine.Object,
              UmbracoContextAccessor.Object,
              _competitionDataSource.Object,
              _authorizationPolicy.Object,
              _emailProtector.Object
                )
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_competition_returns_404()
        {
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).Returns(Task.FromResult<Competition?>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_competition_returns_CompetitionViewModel()
        {
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(new Competition());

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<CompetitionViewModel>(((ViewResult)result).Model);
            }
        }
        [Fact]
        public async Task Model_has_competition_and_authorization()
        {
            var competition = new Competition { CompetitionName = "Example" };
            var authorization = new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditCompetition, true } };
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(competition);
            _authorizationPolicy.Setup(x => x.IsAuthorized(competition)).ReturnsAsync(authorization);

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<CompetitionViewModel>(((ViewResult)result).Model);
            Assert.Equal(competition, model.Competition);
            Assert.Equal(authorization, model.Authorization.CurrentMemberIsAuthorized);
        }

        [Fact]
        public async Task Model_has_metadata_and_breadcrumb()
        {
            var competition = new Competition { CompetitionName = "Example" };
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(competition);

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<CompetitionViewModel>(((ViewResult)result).Model);
            Assert.Equal("Example", model.Metadata.PageTitle);
            Assert.Equal(competition.Description(), model.Metadata.Description);
            Assert.Equal(Constants.Pages.Competitions, model.Breadcrumbs[^1].Name);
        }

        [Fact]
        public async Task Email_addresses_in_introduction_and_contact_details_are_protected_for_signed_out_user()
        {
            var competition = new Competition { Introduction = "<p>intro@example.org</p>", PublicContactDetails = "<p>contact@example.org</p>" };
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(competition);
            _emailProtector.Setup(x => x.ProtectEmailAddresses("<p>intro@example.org</p>", false)).Returns("<p>protected intro</p>");
            _emailProtector.Setup(x => x.ProtectEmailAddresses("<p>contact@example.org</p>", false)).Returns("<p>protected contact</p>");

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<CompetitionViewModel>(((ViewResult)result).Model);
            Assert.Equal("<p>protected intro</p>", model.Competition!.Introduction);
            Assert.Equal("<p>protected contact</p>", model.Competition.PublicContactDetails);
        }
    }
}
