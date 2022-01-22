using System.Collections.Generic;
using Stoolball.Matches;

namespace Stoolball.Web.Matches
{
    /// <remarks>
    /// ASP.NET ModelBinding with Umbraco and StoolballRouter does not support binding to multiple root-level properties,
    /// so use this class as a property of <see cref="EditScorecardViewModel"/> to collect all the data that should be model-bound.
    /// </remarks>
    public class MatchInningsViewModel
    {
        public MatchInnings MatchInnings { get; set; } = new MatchInnings();
        public List<PlayerInningsViewModel> PlayerInningsSearch { get; internal set; } = new List<PlayerInningsViewModel>();
        public List<OverViewModel> OversBowledSearch { get; internal set; } = new List<OverViewModel>();

    }
}