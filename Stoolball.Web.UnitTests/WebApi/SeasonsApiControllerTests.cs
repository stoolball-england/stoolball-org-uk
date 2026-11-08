using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.Web.WebApi;
using Xunit;

namespace Stoolball.Web.UnitTests.WebApi
{
    public class SeasonsApiControllerTests
    {
        private readonly Mock<ISeasonDataSource> _seasonDataSource = new();
        private CompetitionFilter? _filterUsed;

        public SeasonsApiControllerTests()
        {
            _seasonDataSource.Setup(x => x.ReadSeasons(It.IsAny<CompetitionFilter>()))
                .Callback<CompetitionFilter>(f => _filterUsed = f)
                .ReturnsAsync(new List<Season>());
        }

        private SeasonsApiController CreateController() => new(_seasonDataSource.Object);

        [Fact]
        public void Null_season_data_source_throws_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SeasonsApiController(null!));
        }

        [Fact]
        public async Task Query_is_passed_to_data_source()
        {
            var controller = CreateController();

            await controller.Autocomplete("example");

            Assert.Equal("example", _filterUsed!.Query);
        }

        [Fact]
        public async Task No_parameters_gives_unfiltered_query()
        {
            var controller = CreateController();

            await controller.Autocomplete();

            Assert.Null(_filterUsed!.Query);
            Assert.Empty(_filterUsed.MatchTypes);
        }

        [Fact]
        public async Task Valid_match_types_are_filtered_ignoring_case_and_invalid_or_null_types_are_ignored()
        {
            var controller = CreateController();

            await controller.Autocomplete(matchType: ["leaguematch", "NotAMatchType", null!, "KnockoutMatch"]);

            Assert.Equal([MatchType.LeagueMatch, MatchType.KnockoutMatch], _filterUsed!.MatchTypes);
        }

        [Fact]
        public async Task No_seasons_returns_empty_suggestions()
        {
            var controller = CreateController();

            var result = await controller.Autocomplete();

            Assert.Empty(result.suggestions);
        }

        [Fact]
        public async Task Each_season_becomes_a_suggestion_with_full_name_and_season_id_as_data()
        {
            var first = new Season { SeasonId = Guid.NewGuid(), FromYear = 2025, UntilYear = 2026, Competition = new Competition { CompetitionName = "Example league" } };
            var second = new Season { SeasonId = Guid.NewGuid(), FromYear = 2026, UntilYear = 2026, Competition = new Competition { CompetitionName = "Other league" } };
            _seasonDataSource.Setup(x => x.ReadSeasons(It.IsAny<CompetitionFilter>())).ReturnsAsync([first, second]);
            var controller = CreateController();

            var result = await controller.Autocomplete();

            var suggestions = result.suggestions.ToList();
            Assert.Equal(2, suggestions.Count);
            Assert.Equal(first.SeasonFullName(), suggestions[0].value);
            Assert.Equal(first.SeasonId.ToString(), suggestions[0].data);
            Assert.Equal(second.SeasonFullName(), suggestions[1].value);
            Assert.Equal(second.SeasonId.ToString(), suggestions[1].data);
        }
    }
}
