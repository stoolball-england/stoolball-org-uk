using System.Collections.Generic;
using System.Web.Mvc;
using Stoolball.Matches;

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
        void MatchDateIsValidForSqlServer(IEditMatchViewModel model, ModelStateDictionary modelState);
    }
}