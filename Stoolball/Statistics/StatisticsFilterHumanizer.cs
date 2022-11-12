using System.Text;
using Stoolball.Dates;
using Stoolball.Filtering;

namespace Stoolball.Statistics
{
    public class StatisticsFilterHumanizer : BaseFilterHumanizer, IStatisticsFilterHumanizer
    {
        private readonly IDateTimeFormatter _dateTimeFormatter;

        public StatisticsFilterHumanizer(IDateTimeFormatter dateTimeFormatter)
        {
            _dateTimeFormatter = dateTimeFormatter ?? throw new System.ArgumentNullException(nameof(dateTimeFormatter));
        }

        public string MatchingUserFilter(StatisticsFilter filter)
        {
            if (filter == null) return string.Empty;

            var description = new StringBuilder();

            if (!string.IsNullOrEmpty(filter.Team?.TeamName))
            {
                description.Append(" for ").Append(filter.Team.TeamName);
            }

            AppendDateFilter(filter.FromDate, filter.UntilDate, description, _dateTimeFormatter);

            return description.ToString().TrimEnd();
        }

        public string MatchingDefaultFilter(StatisticsFilter filter)
        {
            if (filter == null) return string.Empty;

            var description = new StringBuilder();
            if (filter.Player != null)
            {
                description.Append(" for ").Append(filter.Player.PlayerName());
            }
            if (filter.Club != null)
            {
                description.Append(" for ").Append(filter.Club.ClubName);
            }
            if (!string.IsNullOrEmpty(filter.Team?.TeamName))
            {
                description.Append(" for ").Append(filter.Team.TeamName);
            }
            if (filter.Competition != null)
            {
                description.Append(" in the ").Append(filter.Competition.CompetitionName);
            }
            if (filter.Season != null)
            {
                description.Append(" in the ").Append(filter.Season.SeasonFullName());
            }
            if (filter.MatchLocation != null)
            {
                description.Append(" at ").Append(filter.MatchLocation.NameAndLocalityOrTown());
            }
            return description.ToString().TrimEnd();
        }
    }
}
