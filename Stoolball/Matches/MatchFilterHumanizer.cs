using System.Globalization;
using System.Text;
using Stoolball.Dates;

namespace Stoolball.Matches
{
    public class MatchFilterHumanizer : IMatchFilterHumanizer
    {
        private readonly IDateTimeFormatter _dateTimeFormatter;

        public MatchFilterHumanizer(IDateTimeFormatter dateTimeFormatter)
        {
            _dateTimeFormatter = dateTimeFormatter ?? throw new System.ArgumentNullException(nameof(dateTimeFormatter));
        }

        public string Humanize(MatchFilter filter)
        {
            if (filter == null) return string.Empty;

            var description = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                description.Append(" matching '").Append(filter.Query).Append("' ");
            }
            if (filter.FromDate.HasValue)
            {
                description.Append(" from ").Append(_dateTimeFormatter.FormatDate(filter.FromDate.Value, false, true, false));
                if (!filter.UntilDate.HasValue)
                {
                    description.Append(" onwards");
                }
            }
            if (filter.UntilDate.HasValue)
            {
                description.Append(" up to ").Append(_dateTimeFormatter.FormatDate(filter.UntilDate.Value, false, true, false));
            }

            if (description.Length == 0)
            {
                description.Insert(0, "All " + MatchesAndTournaments(filter).ToLower(CultureInfo.CurrentCulture));
            }
            else
            {
                description.Insert(0, MatchesAndTournaments(filter));
            }
            return description.ToString().TrimEnd();
        }

        private static string MatchesAndTournaments(MatchFilter filter)
        {
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
    }
}
