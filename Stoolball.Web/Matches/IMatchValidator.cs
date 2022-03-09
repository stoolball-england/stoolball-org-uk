using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Web.Matches.Models;

namespace Stoolball.Web.Matches
{
    /// <summary>
    /// Validates the parsed model for a match
    /// </summary>
    public interface IMatchValidator
    {
        void TeamsMustBeDifferent(IEditMatchViewModel model, ModelStateDictionary modelState);
        void AtLeastOneTeamId(IEditMatchViewModel model, ModelStateDictionary modelState);
        void AtLeastOneTeamInMatch(IEnumerable<TeamInMatch> model, ModelStateDictionary modelState);
        void DateIsValidForSqlServer(DateTimeOffset? dateToValidate, ModelStateDictionary modelState, string fieldName, string dateOfWhat);
        void DateIsWithinTheSeason(DateTimeOffset? dateToValidate, Season? season, ModelStateDictionary modelState, string fieldName, string dateOfWhat);
    }
}