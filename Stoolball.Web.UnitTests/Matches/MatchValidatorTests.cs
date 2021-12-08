using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Web.Mvc;
using Moq;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Web.Matches;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class MatchValidatorTests
    {
        [Fact]
        public void TeamsMustBeDifferent_throws_NullReferenceException_if_model_is_null()
        {
            var modelState = new ModelStateDictionary();

            Assert.Throws<ArgumentNullException>(() => new MatchValidator(Mock.Of<ISeasonEstimator>()).TeamsMustBeDifferent(null, modelState));
        }

        [Fact]
        public void TeamsMustBeDifferent_throws_NullReferenceException_if_ModelState_is_null()
        {
            var model = new Mock<IEditMatchViewModel>();

            Assert.Throws<ArgumentNullException>(() => new MatchValidator(Mock.Of<ISeasonEstimator>()).TeamsMustBeDifferent(model.Object, null));
        }

        [Fact]
        public void TeamsMustBeDifferent_is_valid_if_both_teams_are_null()
        {
            var model = new Mock<IEditMatchViewModel>();
            var modelState = new ModelStateDictionary();

            new MatchValidator(Mock.Of<ISeasonEstimator>()).TeamsMustBeDifferent(model.Object, modelState);

            Assert.Empty(modelState.Keys);
        }

        [Fact]
        public void TeamsMustBeDifferent_is_valid_if_only_home_team_is_null()
        {
            var model = new Mock<IEditMatchViewModel>();
            model.Setup(x => x.AwayTeamId).Returns(Guid.NewGuid());
            var modelState = new ModelStateDictionary();

            new MatchValidator(Mock.Of<ISeasonEstimator>()).TeamsMustBeDifferent(model.Object, modelState);

            Assert.Empty(modelState.Keys);
        }

        [Fact]
        public void TeamsMustBeDifferent_is_valid_if_only_away_team_is_null()
        {
            var model = new Mock<IEditMatchViewModel>();
            model.Setup(x => x.HomeTeamId).Returns(Guid.NewGuid());
            var modelState = new ModelStateDictionary();

            new MatchValidator(Mock.Of<ISeasonEstimator>()).TeamsMustBeDifferent(model.Object, modelState);

            Assert.Empty(modelState.Keys);
        }

        [Fact]
        public void TeamsMustBeDifferent_is_valid_if_teams_are_different()
        {
            var model = new Mock<IEditMatchViewModel>();
            model.Setup(x => x.HomeTeamId).Returns(Guid.NewGuid());
            model.Setup(x => x.AwayTeamId).Returns(Guid.NewGuid());
            var modelState = new ModelStateDictionary();

            new MatchValidator(Mock.Of<ISeasonEstimator>()).TeamsMustBeDifferent(model.Object, modelState);

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

            new MatchValidator(Mock.Of<ISeasonEstimator>()).TeamsMustBeDifferent(model.Object, modelState);

            Assert.Contains("AwayTeamId", modelState.Keys);
        }

        [Fact]
        public void AtLeastOneTeamInMatch_throws_NullReferenceException_if_model_is_null()
        {
            var modelState = new ModelStateDictionary();

            Assert.Throws<ArgumentNullException>(() => new MatchValidator(Mock.Of<ISeasonEstimator>()).AtLeastOneTeamInMatch(null, modelState));
        }

        [Fact]
        public void AtLeastOneTeamInMatch_throws_NullReferenceException_if_ModelState_is_null()
        {
            var model = new List<TeamInMatch>();

            Assert.Throws<ArgumentNullException>(() => new MatchValidator(Mock.Of<ISeasonEstimator>()).AtLeastOneTeamInMatch(model, null));
        }

        [Fact]
        public void AtLeastOneTeamInMatch_clears_existing_MatchTeams_errors()
        {
            var model = new List<TeamInMatch>();
            model.Add(new TeamInMatch());
            var modelState = new ModelStateDictionary();
            modelState.AddModelError("Match.Teams", "Error text");
            modelState.AddModelError("Match.Teams.AnyProperty", "Error text");
            modelState.AddModelError("Match.OtherProperty", "Error text");

            new MatchValidator(Mock.Of<ISeasonEstimator>()).AtLeastOneTeamInMatch(model, modelState);

            Assert.Empty(modelState.Where(x => x.Key.StartsWith("Match.Teams", StringComparison.OrdinalIgnoreCase)).SelectMany(x => x.Value.Errors));
            Assert.Single(modelState.Where(x => !x.Key.StartsWith("Match.Teams", StringComparison.OrdinalIgnoreCase)).SelectMany(x => x.Value.Errors));
        }

        [Fact]
        public void AtLeastOneTeamInMatch_marks_MatchTeams_invalid_if_no_teams()
        {
            var model = new List<TeamInMatch>();
            var modelState = new ModelStateDictionary();

            new MatchValidator(Mock.Of<ISeasonEstimator>()).AtLeastOneTeamInMatch(model, modelState);

            Assert.Contains("Match.Teams", modelState.Keys);
        }

        [Fact]
        public void AtLeastOneTeamInMatch_is_valid_with_at_least_one_team()
        {
            var model = new List<TeamInMatch>();
            model.Add(new TeamInMatch());
            var modelState = new ModelStateDictionary();

            new MatchValidator(Mock.Of<ISeasonEstimator>()).AtLeastOneTeamInMatch(model, modelState);

            Assert.Empty(modelState.Keys);
        }

        [Fact]
        public void AtLeastOneTeamId_throws_NullReferenceException_if_model_is_null()
        {
            var modelState = new ModelStateDictionary();

            Assert.Throws<ArgumentNullException>(() => new MatchValidator(Mock.Of<ISeasonEstimator>()).AtLeastOneTeamId(null, modelState));
        }

        [Fact]
        public void AtLeastOneTeamId_throws_NullReferenceException_if_ModelState_is_null()
        {
            var model = new Mock<IEditMatchViewModel>();

            Assert.Throws<ArgumentNullException>(() => new MatchValidator(Mock.Of<ISeasonEstimator>()).AtLeastOneTeamId(model.Object, null));
        }

        [Fact]
        public void AtLeastOneTeamId_marks_HomeTeamId_invalid_if_both_teams_are_null()
        {
            var model = new Mock<IEditMatchViewModel>();
            var modelState = new ModelStateDictionary();

            new MatchValidator(Mock.Of<ISeasonEstimator>()).AtLeastOneTeamId(model.Object, modelState);

            Assert.Contains("HomeTeamId", modelState.Keys);
        }

        [Fact]
        public void AtLeastOneTeamId_is_valid_if_home_team_is_set()
        {
            var model = new Mock<IEditMatchViewModel>();
            model.Setup(x => x.HomeTeamId).Returns(Guid.NewGuid());
            var modelState = new ModelStateDictionary();

            new MatchValidator(Mock.Of<ISeasonEstimator>()).AtLeastOneTeamId(model.Object, modelState);

            Assert.Empty(modelState.Keys);
        }

        [Fact]
        public void AtLeastOneTeamId_is_valid_if_away_team_is_set()
        {
            var model = new Mock<IEditMatchViewModel>();
            model.Setup(x => x.AwayTeamId).Returns(Guid.NewGuid());
            var modelState = new ModelStateDictionary();

            new MatchValidator(Mock.Of<ISeasonEstimator>()).AtLeastOneTeamId(model.Object, modelState);

            Assert.Empty(modelState.Keys);
        }

        [Fact]
        public void DateIsValidForSqlServer_throws_NullReferenceException_if_date_Func_is_null()
        {
            var modelState = new ModelStateDictionary();

            Assert.Throws<ArgumentNullException>(() => new MatchValidator(Mock.Of<ISeasonEstimator>()).DateIsValidForSqlServer(null, modelState, "MatchDate", "match"));
        }

        [Fact]
        public void DateIsValidForSqlServer_throws_NullReferenceException_if_ModelState_is_null()
        {
            Assert.Throws<ArgumentNullException>(() => new MatchValidator(Mock.Of<ISeasonEstimator>()).DateIsValidForSqlServer(() => DateTimeOffset.UtcNow, null, "MatchDate", "match"));
        }

        [Fact]
        public void DateIsValidForSqlServer_marks_field_invalid_if_too_far_in_the_past()
        {
            var modelState = new ModelStateDictionary();
            var fieldName = Guid.NewGuid().ToString();

            new MatchValidator(Mock.Of<ISeasonEstimator>()).DateIsValidForSqlServer(() => SqlDateTime.MinValue.Value.Date.AddDays(-1), modelState, fieldName, "match");

            Assert.Contains(fieldName, modelState.Keys);
        }

        // NOTE: Can't test too far in the future as it's too far for .NET too

        [Fact]
        public void DateIsValidForSqlServer_is_valid_for_SqlServer_minimum()
        {
            var modelState = new ModelStateDictionary();

            new MatchValidator(Mock.Of<ISeasonEstimator>()).DateIsValidForSqlServer(() => SqlDateTime.MinValue.Value.Date, modelState, "MatchDate", "match");

            Assert.Empty(modelState.Keys);
        }

        [Fact]
        public void DateIsValidForSqlServer_is_valid_for_SqlServer_maximum()
        {
            var modelState = new ModelStateDictionary();

            new MatchValidator(Mock.Of<ISeasonEstimator>()).DateIsValidForSqlServer(() => SqlDateTime.MaxValue.Value.Date, modelState, "MatchDate", "match");

            Assert.Empty(modelState.Keys);
        }

        [Fact]
        public void DateIsValidForSqlServer_is_valid_if_MatchDate_is_null()
        {
            var modelState = new ModelStateDictionary();

            new MatchValidator(Mock.Of<ISeasonEstimator>()).DateIsValidForSqlServer(() => null, modelState, "MatchDate", "match");

            Assert.Empty(modelState.Keys);
        }


        public void DateIsWithinTheSeason_throws_NullReferenceException_if_date_Func_is_null()
        {
            var modelState = new ModelStateDictionary();

            Assert.Throws<ArgumentNullException>(() => new MatchValidator(Mock.Of<ISeasonEstimator>()).DateIsWithinTheSeason(null, new Season(), modelState, "MatchDate", "match"));
        }

        [Fact]
        public void DateIsWithinTheSeason_throws_NullReferenceException_if_ModelState_is_null()
        {
            Assert.Throws<ArgumentNullException>(() => new MatchValidator(Mock.Of<ISeasonEstimator>()).DateIsWithinTheSeason(() => DateTimeOffset.UtcNow, new Season(), null, "MatchDate", "match"));
        }

        [Fact]
        public void DateIsWithinTheSeason_is_valid_if_MatchDate_is_null()
        {
            var modelState = new ModelStateDictionary();

            new MatchValidator(Mock.Of<ISeasonEstimator>()).DateIsWithinTheSeason(() => null, new Season(), modelState, "MatchDate", "match");

            Assert.Empty(modelState.Keys);
        }

        [Fact]
        public void DateIsWithinTheSeason_is_valid_if_Season_is_null()
        {
            var modelState = new ModelStateDictionary();

            new MatchValidator(Mock.Of<ISeasonEstimator>()).DateIsWithinTheSeason(() => DateTimeOffset.UtcNow, null, modelState, "MatchDate", "match");

            Assert.Empty(modelState.Keys);
        }

        [Fact]
        public void DateIsWithinTheSeason_estimates_season_for_match()
        {
            var matchDate = DateTimeOffset.UtcNow;
            var estimator = new Mock<ISeasonEstimator>();
            var modelState = new ModelStateDictionary();

            new MatchValidator(estimator.Object).DateIsWithinTheSeason(() => matchDate, new Season(), modelState, "MatchDate", "match");

            estimator.Verify(x => x.EstimateSeasonDates(matchDate), Times.Once);
        }

        [Fact]
        public void DateIsWithinTheSeason_is_valid_if_FromYear_and_UntilYear_match_the_estimate()
        {
            var matchDate = new DateTimeOffset(2020, 12, 31, 18, 0, 0, TimeSpan.Zero);
            var estimator = new Mock<ISeasonEstimator>();
            estimator.Setup(x => x.EstimateSeasonDates(matchDate)).Returns((new DateTimeOffset(2020, 9, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2021, 3, 31, 0, 0, 0, TimeSpan.Zero)));
            var modelState = new ModelStateDictionary();

            new MatchValidator(estimator.Object).DateIsWithinTheSeason(() => matchDate, new Season { FromYear = 2020, UntilYear = 2021 }, modelState, "MatchDate", "match");

            Assert.Empty(modelState.Keys);
        }

        [Fact]
        public void DateIsWithinTheSeason_marks_field_invalid_if_FromYear_does_not_match_the_estimate()
        {
            var matchDate = new DateTimeOffset(2020, 12, 31, 18, 0, 0, TimeSpan.Zero);
            var estimator = new Mock<ISeasonEstimator>();
            estimator.Setup(x => x.EstimateSeasonDates(matchDate)).Returns((new DateTimeOffset(2021, 4, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2021, 8, 31, 0, 0, 0, TimeSpan.Zero)));
            var fieldName = Guid.NewGuid().ToString();
            var modelState = new ModelStateDictionary();

            new MatchValidator(estimator.Object).DateIsWithinTheSeason(() => matchDate, new Season { FromYear = 2020, UntilYear = 2021 }, modelState, fieldName, "match");

            Assert.Contains(fieldName, modelState.Keys);
        }

        [Fact]
        public void DateIsWithinTheSeason_marks_field_invalid_if_UntilYear_does_not_match_the_estimate()
        {
            var matchDate = new DateTimeOffset(2020, 12, 31, 18, 0, 0, TimeSpan.Zero);
            var estimator = new Mock<ISeasonEstimator>();
            estimator.Setup(x => x.EstimateSeasonDates(matchDate)).Returns((new DateTimeOffset(2020, 4, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2020, 8, 31, 0, 0, 0, TimeSpan.Zero)));
            var fieldName = Guid.NewGuid().ToString();
            var modelState = new ModelStateDictionary();

            new MatchValidator(estimator.Object).DateIsWithinTheSeason(() => matchDate, new Season { FromYear = 2020, UntilYear = 2021 }, modelState, fieldName, "match");

            Assert.Contains(fieldName, modelState.Keys);
        }
    }
}
