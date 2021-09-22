using System;
using System.Collections.Generic;
using System.Linq;
using Stoolball.Matches;
using Stoolball.Statistics;

namespace Stoolball.Testing
{
    public class DateRangeGenerator
    {
        public StatisticsFilter SelectDateRangeToTest(IEnumerable<Match> matches)
        {
            var allMatchDates = matches.Select(x => x.StartTime).OrderBy(x => x);
            var oneThirdOfTheTimeBetweenFirstAndLast = TimeSpan.FromTicks((allMatchDates.Last() - allMatchDates.First()).Ticks / 3);
            var ranges = new[] {
                new StatisticsFilter { FromDate = allMatchDates.First(), UntilDate= allMatchDates.First().Add(oneThirdOfTheTimeBetweenFirstAndLast) },
                new StatisticsFilter{ FromDate = allMatchDates.First().Add(oneThirdOfTheTimeBetweenFirstAndLast), UntilDate = allMatchDates.Last().Add(-oneThirdOfTheTimeBetweenFirstAndLast) },
                new StatisticsFilter{ FromDate = allMatchDates.Last().Add(-oneThirdOfTheTimeBetweenFirstAndLast), UntilDate = allMatchDates.Last() }
            };

            return ranges[new Random().Next(3)];
        }
    }
}
