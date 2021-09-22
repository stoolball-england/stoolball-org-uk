using System.Text;
using Stoolball.Dates;
using Stoolball.Filtering;

namespace Stoolball.Matches
{
    public class MatchFilterHumanizer : BaseFilterHumanizer, IMatchFilterHumanizer
    {
        private readonly IDateTimeFormatter _dateTimeFormatter;

        public MatchFilterHumanizer(IDateTimeFormatter dateTimeFormatter)
        {
            _dateTimeFormatter = dateTimeFormatter ?? throw new System.ArgumentNullException(nameof(dateTimeFormatter));
        }

        public string MatchingFilter(MatchFilter filter)
        {
            if (filter == null) return string.Empty;

            var description = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                description.Append(" matching '").Append(filter.Query).Append("' ");
            }
            AppendDateFilter(filter.FromDate, filter.UntilDate, description, _dateTimeFormatter);

            return description.ToString().TrimEnd();
        }

        public string MatchesAndTournaments(MatchFilter filter)
        {
            if (filter == null) return string.Empty;

            if (filter.IncludeMatches && !filter.IncludeTournaments)
            {
                return "Matches";
            }
            else if (filter.IncludeMatches && filter.IncludeTournaments)
            {
                return "Matches and tournaments";
            }
            else if (filter.IncludeTournaments)
            {
                return "Tournaments";
            }
            return string.Empty;
        }

        public string MatchesAndTournamentsMatchingFilter(MatchFilter filter)
        {
            return EntitiesMatchingFilter(MatchesAndTournaments(filter), MatchingFilter(filter));
        }
    }
}
