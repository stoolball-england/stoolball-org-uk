using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Web.Mvc;
using Moq;
using Stoolball.Matches;
using Stoolball.Web.Matches;
using Xunit;

namespace Stoolball.Web.Tests.Matches
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
            var model = new List<TeamInMatch>();
            model.Add(new TeamInMatch());
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
            var model = new List<TeamInMatch>();
            model.Add(new TeamInMatch());
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
        public void MatchDateIsValidForSqlServer_throws_NullReferenceException_if_model_is_null()
        {
            var modelState = new ModelStateDictionary();

            Assert.Throws<ArgumentNullException>(() => new MatchValidator().MatchDateIsValidForSqlServer(null, modelState));
        }

        [Fact]
        public void MatchDaMatchDateIsValidForSqlServerteIsValidForSqlServer_throws_NullReferenceException_if_ModelState_is_null()
        {
            var model = new Mock<IEditMatchViewModel>();

            Assert.Throws<ArgumentNullException>(() => new MatchValidator().MatchDateIsValidForSqlServer(model.Object, null));
        }

        [Fact]
        public void MatchDateIsValidForSqlServer_marks_MatchDate_invalid_if_too_far_in_the_past()
        {
            var model = new Mock<IEditMatchViewModel>();
            model.Setup(x => x.MatchDate).Returns(SqlDateTime.MinValue.Value.Date.AddDays(-1));
            var modelState = new ModelStateDictionary();

            new MatchValidator().MatchDateIsValidForSqlServer(model.Object, modelState);

            Assert.Contains("MatchDate", modelState.Keys);
        }

        // NOTE: Can't test too far in the future as it's too far for .NET too

        [Fact]
        public void MatchDateIsValidForSqlServer_is_valid_for_SqlServer_minimum()
        {
            var model = new Mock<IEditMatchViewModel>();
            model.Setup(x => x.MatchDate).Returns(SqlDateTime.MinValue.Value.Date);
            var modelState = new ModelStateDictionary();

            new MatchValidator().MatchDateIsValidForSqlServer(model.Object, modelState);

            Assert.Empty(modelState.Keys);
        }

        [Fact]
        public void MatchDateIsValidForSqlServer_is_valid_for_SqlServer_maximum()
        {
            var model = new Mock<IEditMatchViewModel>();
            model.Setup(x => x.MatchDate).Returns(SqlDateTime.MaxValue.Value.Date);
            var modelState = new ModelStateDictionary();

            new MatchValidator().MatchDateIsValidForSqlServer(model.Object, modelState);

            Assert.Empty(modelState.Keys);
        }

        [Fact]
        public void MatchDateIsValidForSqlServer_is_valid_if_MatchDate_is_null()
        {
            var model = new Mock<IEditMatchViewModel>();
            var modelState = new ModelStateDictionary();

            new MatchValidator().MatchDateIsValidForSqlServer(model.Object, modelState);

            Assert.Empty(modelState.Keys);
        }
    }
}
