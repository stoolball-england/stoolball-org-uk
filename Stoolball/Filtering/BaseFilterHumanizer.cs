using System;
using System.Text;
using Stoolball.Dates;

namespace Stoolball.Filtering
{
    public abstract class BaseFilterHumanizer
    {
        public string EntitiesMatchingFilter(string entities, string matchingFilter)
        {
            if (matchingFilter?.Length == 0)
            {
                return string.Empty;
            }
            else
            {
                return entities + matchingFilter;
            }
        }

        protected static void AppendDateFilter(DateTimeOffset? fromDate, DateTimeOffset? untilDate, StringBuilder description, IDateTimeFormatter dateTimeFormatter)
        {
            if (dateTimeFormatter is null)
            {
                throw new ArgumentNullException(nameof(dateTimeFormatter));
            }

            if (description is null)
            {
                throw new ArgumentNullException(nameof(description));
            }

            if (fromDate.HasValue)
            {
                description.Append(" from ").Append(dateTimeFormatter.FormatDate(fromDate.Value, false, true, false));
                if (!untilDate.HasValue)
                {
                    description.Append(" onwards");
                }
            }
            if (untilDate.HasValue)
            {
                description.Append(" up to ").Append(dateTimeFormatter.FormatDate(untilDate.Value, false, true, false));
            }


        }
    }
}
