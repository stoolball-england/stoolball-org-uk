using System;
using System.Collections.Generic;
using System.Linq;
using Stoolball.Matches;

namespace Stoolball.Testing
{
    internal class DateRangeGenerator
    {
        internal (DateTimeOffset fromDate, DateTimeOffset untilDate) SelectDateRangeToTest(IEnumerable<Match> matches)
        {
            var allMatchDates = matches.Select(x => x.StartTime).OrderBy(x => x);
            var oneThirdOfTheTimeBetweenFirstAndLast = TimeSpan.FromTicks((allMatchDates.Last() - allMatchDates.First()).Ticks / 3);
            var ranges = new[] {
                (allMatchDates.First(), allMatchDates.First().Add(oneThirdOfTheTimeBetweenFirstAndLast)),
                (allMatchDates.First().Add(oneThirdOfTheTimeBetweenFirstAndLast), allMatchDates.Last().Add(-oneThirdOfTheTimeBetweenFirstAndLast)),
                (allMatchDates.Last().Add(-oneThirdOfTheTimeBetweenFirstAndLast), allMatchDates.Last())
            };

            return ranges[new Random().Next(3)];
        }
    }
}
