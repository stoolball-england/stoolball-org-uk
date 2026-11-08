using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Comments;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Dates;
using Stoolball.Email;
using Stoolball.Html;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Configuration;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Umbraco.Cms.Core.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class TournamentControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ITournamentDataSource> _tournamentDataSource = new();
        private readonly Mock<IMatchListingDataSource> _matchDataSource = new();
        private readonly Mock<IMatchFilterFactory> _matchFilterFactory = new();
        private readonly Mock<ICommentsDataSource<Tournament>> _commentsDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Tournament>> _authorizationPolicy = new();
        private readonly Mock<IDateTimeFormatter> _dateFormatter = new();
        private readonly Mock<IApiKeyProvider> _apiKeyProvider = new();
        private readonly Mock<IEmailProtector> _emailProtector = new();
        private readonly Mock<IBadLanguageFilter> _badLanguageFilter = new();
        private readonly MatchFilter _matchFilter = new();
        private readonly Tournament _tournament = new()
        {
            TournamentId = Guid.NewGuid(),
            TournamentName = "Example tournament",
            TournamentRoute = "/tournaments/example",
            TournamentNotes = "<p>Notes</p>",
            StartTime = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero),
            Seasons = [new Season { Competition = new Competition { CompetitionName = "Example league" } }]
        };

        public TournamentControllerTests()
        {
            _tournamentDataSource.Setup(x => x.ReadTournamentByRoute(It.IsAny<string>())).ReturnsAsync(_tournament);
            _commentsDataSource.Setup(x => x.ReadComments(_tournament.TournamentId!.Value)).ReturnsAsync(new List<HtmlComment>());
            _matchFilterFactory.Setup(x => x.MatchesForTournament(_tournament.TournamentId!.Value)).Returns((_matchFilter, MatchSortOrder.MatchDateEarliestFirst));
            _matchDataSource.Setup(x => x.ReadMatchListings(It.IsAny<MatchFilter>(), It.IsAny<MatchSortOrder>())).ReturnsAsync(new List<MatchListing>());
            _dateFormatter.Setup(x => x.FormatDate(_tournament.StartTime, false, false, false)).Returns("1 June 2026");
            _emailProtector.Setup(x => x.ProtectEmailAddresses(It.IsAny<string>(), It.IsAny<bool>())).Returns((string html, bool authenticated) => $"{html} protected {authenticated}");
            _badLanguageFilter.Setup(x => x.Filter(It.IsAny<string>())).Returns((string text) => $"{text} filtered");
        }

        private TournamentController CreateController()
        {
            return new TournamentController(
                Mock.Of<ILogger<TournamentController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _tournamentDataSource.Object,
                _matchDataSource.Object,
                _matchFilterFactory.Object,
                _commentsDataSource.Object,
                _authorizationPolicy.Object,
                _dateFormatter.Object,
                _apiKeyProvider.Object,
                _emailProtector.Object,
                _badLanguageFilter.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private async Task<TournamentViewModel> ReadModel()
        {
            using var controller = CreateController();

            var result = await controller.Index();

            return Assert.IsType<TournamentViewModel>(((ViewResult)result).Model);
        }

        [Fact]
        public async Task Route_not_matching_tournament_returns_404()
        {
            _tournamentDataSource.Setup(x => x.ReadTournamentByRoute(It.IsAny<string>())).Returns(Task.FromResult<Tournament>(null!));

            using var controller = CreateController();

            var result = await controller.Index();

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Route_matching_tournament_returns_TournamentViewModel_with_tournament()
        {
            var model = await ReadModel();

            Assert.Equal(_tournament, model.Tournament);
        }

        [Fact]
        public async Task Model_has_Google_Maps_API_key()
        {
            _apiKeyProvider.Setup(x => x.GetApiKey("GoogleMaps")).Returns("example-key");

            var model = await ReadModel();

            Assert.Equal("example-key", model.GoogleMapsApiKey);
        }

        [Fact]
        public async Task Model_has_authorization()
        {
            var authorization = new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditTournament, true } };
            _authorizationPolicy.Setup(x => x.IsAuthorized(_tournament)).ReturnsAsync(authorization);

            var model = await ReadModel();

            Assert.Equal(authorization, model.Authorization.CurrentMemberIsAuthorized);
        }

        [Fact]
        public async Task Comments_are_read_for_the_tournament()
        {
            var comments = new List<HtmlComment> { new() { Comment = "Comment one" }, new() { Comment = "Comment two" } };
            _commentsDataSource.Setup(x => x.ReadComments(_tournament.TournamentId!.Value)).ReturnsAsync(comments);

            var model = await ReadModel();

            Assert.Equal(comments, model.Tournament!.Comments);
            _commentsDataSource.Verify(x => x.ReadComments(_tournament.TournamentId!.Value), Times.Once);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Comments_have_email_addresses_protected_and_bad_language_filtered(bool authenticated)
        {
            if (authenticated)
            {
                SetupCurrentMember(Mock.Of<IMember>(x => x.Name == "Jo Bloggs"));
            }
            var comments = new List<HtmlComment> { new() { Comment = "Comment one" }, new() { Comment = "Comment two" } };
            _commentsDataSource.Setup(x => x.ReadComments(_tournament.TournamentId!.Value)).ReturnsAsync(comments);

            var model = await ReadModel();

            Assert.Equal([$"Comment one protected {authenticated} filtered", $"Comment two protected {authenticated} filtered"], model.Tournament!.Comments.Select(x => x.Comment));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Tournament_notes_have_email_addresses_protected(bool authenticated)
        {
            if (authenticated)
            {
                SetupCurrentMember(Mock.Of<IMember>(x => x.Name == "Jo Bloggs"));
            }

            var model = await ReadModel();

            Assert.Equal($"<p>Notes</p> protected {authenticated}", model.Tournament!.TournamentNotes);
        }

        [Fact]
        public async Task Matches_are_read_using_the_filter_for_the_tournament_without_dates_or_highlighting()
        {
            var matches = new List<MatchListing> { new() { MatchName = "Match one" } };
            _matchDataSource.Setup(x => x.ReadMatchListings(_matchFilter, MatchSortOrder.MatchDateEarliestFirst)).ReturnsAsync(matches);

            var model = await ReadModel();

            Assert.Equal(matches, model.Matches.Matches);
            Assert.False(model.Matches.ShowMatchDate);
            Assert.False(model.Matches.HighlightNextMatch);
        }

        [Fact]
        public async Task Model_has_page_title_description_and_breadcrumbs()
        {
            var model = await ReadModel();

            Assert.Equal("Example tournament (Mixed), 1 June 2026", model.Metadata.PageTitle);
            Assert.Equal(_tournament.Description(), model.Metadata.Description);
            Assert.Equal(Constants.Pages.Tournaments, model.Breadcrumbs.Last().Name);
            Assert.Equal(Constants.Pages.TournamentsUrl, model.Breadcrumbs.Last().Url?.ToString());
        }
    }
}
