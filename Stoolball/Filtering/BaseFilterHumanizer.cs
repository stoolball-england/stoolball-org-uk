using System;
using System.Globalization;
using System.Text;
using Stoolball.Dates;

namespace Stoolball.Filtering
{
    public abstract class BaseFilterHumanizer
    {
        protected static string EntitiesMatchingFilter(string entities, string matchingFilter)
        {
            if (matchingFilter?.Length == 0)
            {
                matchingFilter = "All " + entities?.ToLower(CultureInfo.CurrentCulture) + matchingFilter;
            }
            else
            {
                matchingFilter = entities + matchingFilter;
            }
            return matchingFilter;
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
