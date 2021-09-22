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

        public string StatisticsMatchingFilter(StatisticsFilter filter)
        {
            return EntitiesMatchingFilter("Statistics", MatchingFilter(filter));
        }

        public string MatchingFilter(StatisticsFilter filter)
        {
            if (filter == null) return string.Empty;

            var description = new StringBuilder();

            AppendDateFilter(filter.FromDate, filter.UntilDate, description, _dateTimeFormatter);

            return description.ToString().TrimEnd();
        }
    }
}
