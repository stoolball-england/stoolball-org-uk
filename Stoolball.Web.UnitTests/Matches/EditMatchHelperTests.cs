using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class EditMatchHelperTests
    {
        private readonly Dictionary<string, StringValues> _form = new() { { "MatchDate", "2026-06-01" } };
        private readonly ModelStateDictionary _modelState = new();
        private readonly EditFriendlyMatchViewModel _model = new() { Match = new Stoolball.Matches.Match() };

        private void ConfigureModelFromRequestData()
        {
            new EditMatchHelper(Mock.Of<ISeasonDataSource>()).ConfigureModelFromRequestData(_model, new FormCollection(_form), _modelState);
        }

        [Fact]
        public void Date_and_start_time_set_known_start_time()
        {
            _form["StartTime"] = "14:30";

            ConfigureModelFromRequestData();

            Assert.Equal(DateTimeOffset.Parse("2026-06-01").AddHours(14).AddMinutes(30), _model.Match!.StartTime);
            Assert.True(_model.Match.StartTimeIsKnown);
            Assert.Equal(DateTimeOffset.Parse("2026-06-01"), _model.MatchDate);
            Assert.Equal(new TimeSpan(14, 30, 0), _model.StartTime!.Value.TimeOfDay);
        }

        [Fact]
        public void Date_without_start_time_sets_unknown_start_time_of_7pm_on_that_date()
        {
            _model.Match!.StartTimeIsKnown = true;

            ConfigureModelFromRequestData();

            Assert.Equal(DateTimeOffset.Parse("2026-06-01").AddHours(19), _model.Match!.StartTime);
            Assert.False(_model.Match.StartTimeIsKnown);
            Assert.Null(_model.StartTime);
        }

        [Fact]
        public void Invalid_start_time_is_a_model_error()
        {
            _form["StartTime"] = "not-a-time";

            ConfigureModelFromRequestData();

            Assert.Contains(_modelState["StartTime"]!.Errors, x => x.ErrorMessage == "Enter a time in 24-hour HH:MM format.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not-a-date")]
        public void Missing_or_invalid_date_is_a_model_error(string? date)
        {
            if (date is null) { _form.Remove("MatchDate"); } else { _form["MatchDate"] = date; }

            ConfigureModelFromRequestData();

            Assert.Contains(_modelState["MatchDate"]!.Errors, x => x.ErrorMessage == "Enter a date in YYYY-MM-DD format.");
        }
    }
}
