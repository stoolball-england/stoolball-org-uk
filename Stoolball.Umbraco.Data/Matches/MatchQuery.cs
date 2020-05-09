using Stoolball.Matches;
using System;
using System.Collections.Generic;

namespace Stoolball.Umbraco.Data.Matches
{
    public class MatchQuery
    {

        public List<Guid> TeamIds { get; internal set; } = new List<Guid>();
        public List<Guid> SeasonIds { get; internal set; } = new List<Guid>();
        public List<MatchType> MatchTypes { get; internal set; } = new List<MatchType>();

        public List<MatchType> ExcludeMatchTypes { get; internal set; } = new List<MatchType>();
        public DateTimeOffset? FromDate { get; set; }
        public Guid? TournamentId { get; set; }
        public List<Guid> MatchLocationIds { get; internal set; } = new List<Guid>();
    }
}