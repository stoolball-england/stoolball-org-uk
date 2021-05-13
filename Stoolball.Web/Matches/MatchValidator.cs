using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using Stoolball.Competitions;
using Stoolball.Matches;

namespace Stoolball.Web.Matches
{
    /// <inheritdoc/>
    public class MatchValidator : IMatchValidator
    {
        private readonly ISeasonEstimator _seasonEstimator;

        public MatchValidator(ISeasonEstimator seasonEstimator)
        {
            _seasonEstimator = seasonEstimator ?? throw new ArgumentNullException(nameof(seasonEstimator));
        }

        public void TeamsMustBeDifferent(IEditMatchViewModel model, ModelStateDictionary modelState)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (modelState is null)
            {
                throw new ArgumentNullException(nameof(modelState));
            }

            if (model.HomeTeamId.HasValue && model.HomeTeamId == model.AwayTeamId)
            {
                modelState.AddModelError("AwayTeamId", "The away team cannot be the same as the home team");
            }
        }

        public void AtLeastOneTeamId(IEditMatchViewModel model, ModelStateDictionary modelState)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (modelState is null)
            {
                throw new ArgumentNullException(nameof(modelState));
            }

            if (!model.HomeTeamId.HasValue && !model.AwayTeamId.HasValue)
            {
                modelState.AddModelError("HomeTeamId", "Please select at least one team");
            }
        }

        public void AtLeastOneTeamInMatch(IEnumerable<TeamInMatch> model, ModelStateDictionary modelState)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (modelState is null)
            {
                throw new ArgumentNullException(nameof(modelState));
            }

            // We're not interested in validating the details of the selected teams
            foreach (var key in modelState.Keys.Where(x => x.StartsWith("Match.Teams", StringComparison.OrdinalIgnoreCase)))
            {
                modelState[key].Errors.Clear();
            }
            if (!model.Any())
            {
                modelState.AddModelError("Match.Teams", "Please invite at least one team.");
            }
        }

        public void DateIsValidForSqlServer(Func<DateTimeOffset?> dateToValidate, ModelStateDictionary modelState, string fieldName, string dateOfWhat)
        {
            if (dateToValidate is null)
            {
                throw new ArgumentNullException(nameof(dateToValidate));
            }

            if (modelState is null)
            {
                throw new ArgumentNullException(nameof(modelState));
            }

            if (dateToValidate() < SqlDateTime.MinValue.Value.Date || dateToValidate() > SqlDateTime.MaxValue.Value.Date)
            {
                modelState.AddModelError(fieldName, $"The {dateOfWhat} date must be between {SqlDateTime.MinValue.Value.Date.ToString("d MMMM yyyy", CultureInfo.CurrentCulture)} and {SqlDateTime.MaxValue.Value.Date.ToString("d MMMM yyyy", CultureInfo.CurrentCulture)}.");
            }
        }

        public void DateIsWithinTheSeason(Func<DateTimeOffset?> dateToValidate, Season season, ModelStateDictionary modelState, string fieldName, string dateOfWhat)
        {
            if (dateToValidate is null)
            {
                throw new ArgumentNullException(nameof(dateToValidate));
            }

            if (modelState is null)
            {
                throw new ArgumentNullException(nameof(modelState));
            }

            if (dateToValidate().HasValue && season != null)
            {
                var seasonForMatch = _seasonEstimator.EstimateSeasonDates(dateToValidate().Value);
                if (seasonForMatch.fromDate.Year != season.FromYear || seasonForMatch.untilDate.Year != season.UntilYear)
                {
                    modelState.AddModelError(fieldName, $"The {dateOfWhat} date is not in the {season.SeasonFullName()}");
                }
            }
        }
    }
}
