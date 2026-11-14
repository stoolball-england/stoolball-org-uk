using System.Collections.Generic;
using System.Linq;
using Stoolball.Statistics;
using Xunit;

namespace Stoolball.UnitTests.Statistics
{
    public class StatisticsResultsTests
    {
        [Fact]
        public void Enumerates_results_on_the_page_but_not_the_previous_result()
        {
            List<StatisticsResult<int>> page = [new() { Result = 2 }, new() { Result = 3 }];
            var previous = new StatisticsResult<int> { Result = 1 };

            var results = new StatisticsResults<int>(page, previous);

            Assert.Equal(page, results.ToList());
            Assert.Equal(page, results.Results);
            Assert.Same(previous, results.PreviousResult);
        }

        [Fact]
        public void Previous_result_defaults_to_null()
        {
            var results = new StatisticsResults<int>([new StatisticsResult<int>()]);

            Assert.Null(results.PreviousResult);
        }
    }
}
