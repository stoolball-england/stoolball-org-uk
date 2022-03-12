using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class MatchValidatorTests
    {
        [Fact]
        public void TeamsMustBeDifferent_throws_NullReferenceException_if_model_is_null()
        {
            var modelState = new ModelStateDictionary();

            Assert.Throws<ArgumentNullException>(() => new MatchValidator().TeamsMustBeDifferent(null, modelState));
        }

        [Fact]
        public void TeamsMustBeDifferent_throws_NullReferenceException_if_ModelState_is_null()
        {
            var model = new Mock<IEditMatchViewModel>();

            Assert.Throws<ArgumentNullException>(() => new MatchValidator().TeamsMustBeDifferent(model.Object, null));
        }

        [Fact]
        public void TeamsMustBeDifferent_is_valid_if_both_teams_are_null()
        {
            var model = new Mock<IEditMatchViewModel>();
            var modelState = new ModelStateDictionary();

            new MatchValidator().TeamsMustBeDifferent(model.Object, modelState);

            Assert.Empty(modelState.Keys);
        }

        [Fact]
        public void TeamsMustBeDifferent_is_valid_if_only_home_team_is_null()
        {
            var model = new Mock<IEditMatchViewModel>();
            model.Setup(x => x.AwayTeamId).Returns(Guid.NewGuid());
            var modelState = new ModelStateDictionary();

            new MatchValidator().TeamsMustBeDifferent(model.Object, modelState);

            Assert.Empty(modelState.Keys);
        }

        [Fact]
        public void TeamsMustBeDifferent_is_valid_if_only_away_team_is_null()
        {
            var model = new Mock<IEditMatchViewModel>();
            model.Setup(x => x.HomeTeamId).Returns(Guid.NewGuid());
            var modelState = new ModelStateDictionary();

            new MatchValidator().TeamsMustBeDifferent(model.Object, modelState);

            Assert.Empty(modelState.Keys);
        }

        [Fact]
        public void TeamsMustBeDifferent_is_valid_if_teams_are_different()
        {
            var model = new Mock<IEditMatchViewModel>();
            model.Setup(x => x.HomeTeamId).Returns(Guid.NewGuid());
            model.Setup(x => x.AwayTeamId).Returns(Guid.NewGuid());
            var modelState = new ModelStateDictionary();

            new MatchValidator().TeamsMustBeDifferent(model.Object, modelState);

            Assert.Empty(modelState.Keys);
        }

        [Fact]
        public void TeamsMustBeDifferent_marks_AwayTeamId_invalid_if_teams_are_the_same()
        {
            var teamId = Guid.NewGuid();
            var model = new Mock<IEditMatchViewModel>();
            model.Setup(x => x.HomeTeamId).Returns(teamId);
            model.Setup(x => x.AwayTeamId).Returns(teamId);
            var modelState = new ModelStateDictionary();

            new MatchValidator().TeamsMustBeDifferent(model.Object, modelState);

            Assert.Contains("AwayTeamId", modelState.Keys);
        }

        [Fact]
        public void AtLeastOneTeamInMatch_throws_NullReferenceException_if_model_is_null()
        {
            var modelState = new ModelStateDictionary();

            Assert.Throws<ArgumentNullException>(() => new MatchValidator().AtLeastOneTeamInMatch(null, modelState));
        }

        [Fact]
        public void AtLeastOneTeamInMatch_throws_NullReferenceException_if_ModelState_is_null()
        {
            var model = new List<TeamInMatch>();

            Assert.Throws<ArgumentNullException>(() => new MatchValidator().AtLeastOneTeamInMatch(model, null));
        }

        [Fact]
        public void AtLeastOneTeamInMatch_clears_existing_MatchTeams_errors()
        {
            var model = new List<TeamInMatch>
            {
                new TeamInMatch()
            };
            var modelState = new ModelStateDictionary();
            modelState.AddModelError("Match.Teams", "Error text");
            modelState.AddModelError("Match.Teams.AnyProperty", "Error text");
            modelState.AddModelError("Match.OtherProperty", "Error text");

            new MatchValidator().AtLeastOneTeamInMatch(model, modelState);

            Assert.Empty(modelState.Where(x => x.Key.StartsWith("Match.Teams", StringComparison.OrdinalIgnoreCase)).SelectMany(x => x.Value.Errors));
            Assert.Single(modelState.Where(x => !x.Key.StartsWith("Match.Teams", StringComparison.OrdinalIgnoreCase)).SelectMany(x => x.Value.Errors));
        }

        [Fact]
        public void AtLeastOneTeamInMatch_marks_MatchTeams_invalid_if_no_teams()
        {
            var model = new List<TeamInMatch>();
            var modelState = new ModelStateDictionary();

            new MatchValidator().AtLeastOneTeamInMatch(model, modelState);

            Assert.Contains("Match.Teams", modelState.Keys);
        }

        [Fact]
        public void AtLeastOneTeamInMatch_is_valid_with_at_least_one_team()
        {
            var model = new List<TeamInMatch>
            {
                new TeamInMatch()
            };
            var modelState = new ModelStateDictionary();

            new MatchValidator().AtLeastOneTeamInMatch(model, modelState);

            Assert.Empty(modelState.Keys);
        }

        [Fact]
        public void AtLeastOneTeamId_throws_NullReferenceException_if_model_is_null()
        {
            var modelState = new ModelStateDictionary();

            Assert.Throws<ArgumentNullException>(() => new MatchValidator().AtLeastOneTeamId(null, modelState));
        }

        [Fact]
        public void AtLeastOneTeamId_throws_NullReferenceException_if_ModelState_is_null()
        {
            var model = new Mock<IEditMatchViewModel>();

            Assert.Throws<ArgumentNullException>(() => new MatchValidator().AtLeastOneTeamId(model.Object, null));
        }

        [Fact]
        public void AtLeastOneTeamId_marks_HomeTeamId_invalid_if_both_teams_are_null()
        {
            var model = new Mock<IEditMatchViewModel>();
            var modelState = new ModelStateDictionary();

            new MatchValidator().AtLeastOneTeamId(model.Object, modelState);

            Assert.Contains("HomeTeamId", modelState.Keys);
        }

        [Fact]
        public void AtLeastOneTeamId_is_valid_if_home_team_is_set()
        {
            var model = new Mock<IEditMatchViewModel>();
            model.Setup(x => x.HomeTeamId).Returns(Guid.NewGuid());
            var modelState = new ModelStateDictionary();

            new MatchValidator().AtLeastOneTeamId(model.Object, modelState);

            Assert.Empty(modelState.Keys);
        }

        [Fact]
        public void AtLeastOneTeamId_is_valid_if_away_team_is_set()
        {
            var model = new Mock<IEditMatchViewModel>();
            model.Setup(x => x.AwayTeamId).Returns(Guid.NewGuid());
            var modelState = new ModelStateDictionary();

            new MatchValidator().AtLeastOneTeamId(model.Object, modelState);

            Assert.Empty(modelState.Keys);
        }

        [Fact]
        public void DateIsValidForSqlServer_throws_NullReferenceException_if_ModelState_is_null()
        {
            Assert.Throws<ArgumentNullException>(() => new MatchValidator().DateIsValidForSqlServer(DateTimeOffset.UtcNow, null, "MatchDate", "match"));
        }

        [Fact]
        public void DateIsValidForSqlServer_marks_field_invalid_if_too_far_in_the_past()
        {
            var modelState = new ModelStateDictionary();
            var fieldName = Guid.NewGuid().ToString();

            new MatchValidator().DateIsValidForSqlServer(SqlDateTime.MinValue.Value.Date.AddDays(-1), modelState, fieldName, "match");

            Assert.Contains(fieldName, modelState.Keys);
        }

        // NOTE: Can't test too far in the future as it's too far for .NET too

        [Fact]
        public void DateIsValidForSqlServer_is_valid_for_SqlServer_minimum()
        {
            var modelState = new ModelStateDictionary();

            new MatchValidator().DateIsValidForSqlServer(SqlDateTime.MinValue.Value.Date, modelState, "MatchDate", "match");

            Assert.Empty(modelState.Keys);
        }

        [Fact]
        public void DateIsValidForSqlServer_is_valid_for_SqlServer_maximum()
        {
            var modelState = new ModelStateDictionary();

            new MatchValidator().DateIsValidForSqlServer(SqlDateTime.MaxValue.Value.Date, modelState, "MatchDate", "match");

            Assert.Empty(modelState.Keys);
        }

        [Fact]
        public void DateIsValidForSqlServer_is_valid_if_date_is_null()
        {
            var modelState = new ModelStateDictionary();

            new MatchValidator().DateIsValidForSqlServer(null, modelState, "MatchDate", "match");

            Assert.Empty(modelState.Keys);
        }

        [Fact]
        public void DateIsWithinTheSeason_throws_NullReferenceException_if_ModelState_is_null()
        {
            Assert.Throws<ArgumentNullException>(() => new MatchValidator().DateIsWithinTheSeason(DateTimeOffset.UtcNow, new Season(), null, "MatchDate", "match"));
        }

        [Fact]
        public void DateIsWithinTheSeason_is_valid_if_MatchDate_is_null()
        {
            var modelState = new ModelStateDictionary();

            new MatchValidator().DateIsWithinTheSeason(null, new Season(), modelState, "MatchDate", "match");

            Assert.Empty(modelState.Keys);
        }

        [Fact]
        public void DateIsWithinTheSeason_is_valid_if_Season_is_null()
        {
            var modelState = new ModelStateDictionary();

            new MatchValidator().DateIsWithinTheSeason(DateTimeOffset.UtcNow, null, modelState, "MatchDate", "match");

            Assert.Empty(modelState.Keys);
        }

        [Theory]
        [InlineData(2019, 12, 31, false)]
        [InlineData(2020, 1, 1, true)]
        [InlineData(2020, 6, 30, true)]
        [InlineData(2020, 12, 31, true)]
        [InlineData(2021, 1, 1, false)]
        public void DateIsWithinTheSeason_is_valid_if_its_the_same_calendar_year_as_a_single_year_season(int year, int month, int day, bool shouldBeValid)
        {
            var matchDate = new DateTimeOffset(year, month, day, 18, 0, 0, TimeSpan.Zero);
            var fieldName = Guid.NewGuid().ToString();
            var modelState = new ModelStateDictionary();

            new MatchValidator().DateIsWithinTheSeason(matchDate, new Season { FromYear = 2020, UntilYear = 2020 }, modelState, fieldName, "match");

            if (shouldBeValid)
            {
                Assert.Empty(modelState.Keys);
            }
            else
            {
                Assert.Contains(fieldName, modelState.Keys);
            }
        }

        [Theory]
        [InlineData(2020, 6, 30, false)]
        [InlineData(2020, 7, 1, true)]
        [InlineData(2020, 12, 31, true)]
        [InlineData(2021, 1, 1, true)]
        [InlineData(2021, 6, 30, true)]
        [InlineData(2021, 7, 1, false)]
        public void DateIsWithinTheSeason_marks_field_invalid_if_more_than_6_months_either_way_in_a_season_spanning_2_years(int year, int month, int day, bool shouldBeValid)
        {
            var matchDate = new DateTimeOffset(year, month, day, 18, 0, 0, TimeSpan.Zero);
            var fieldName = Guid.NewGuid().ToString();
            var modelState = new ModelStateDictionary();

            new MatchValidator().DateIsWithinTheSeason(matchDate, new Season { FromYear = 2020, UntilYear = 2021 }, modelState, fieldName, "match");

            if (shouldBeValid)
            {
                Assert.Empty(modelState.Keys);
            }
            else
            {
                Assert.Contains(fieldName, modelState.Keys);
            }
        }
    }
}
