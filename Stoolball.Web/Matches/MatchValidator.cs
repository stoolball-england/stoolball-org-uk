using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using Stoolball.Matches;

namespace Stoolball.Web.Matches
{
    /// <inheritdoc/>
    public class MatchValidator : IMatchValidator
    {
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

        public void MatchDateIsValidForSqlServer(IEditMatchViewModel model, ModelStateDictionary modelState)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (modelState is null)
            {
                throw new ArgumentNullException(nameof(modelState));
            }

            if (model.MatchDate < SqlDateTime.MinValue.Value.Date || model.MatchDate > SqlDateTime.MaxValue.Value.Date)
            {
                modelState.AddModelError("MatchDate", $"The match date must be between {SqlDateTime.MinValue.Value.Date.ToString("d MMMM yyyy", CultureInfo.CurrentCulture)} and {SqlDateTime.MaxValue.Value.Date.ToString("d MMMM yyyy", CultureInfo.CurrentCulture)}.");
            }
        }
    }
}
