using Stoolball.Navigation;
using Xunit;

namespace Stoolball.UnitTests.Navigation
{
    public class PagingTests
    {
        [Theory]
        [InlineData(1, 50, 0)]
        [InlineData(2, 50, 50)]
        [InlineData(3, 5, 10)]
        [InlineData(0, 50, 0)]
        [InlineData(-5, 50, 0)]
        [InlineData(42949674, 50, 2147483650)] // 50 * 42949673 overflows an int
        [InlineData(int.MaxValue, 50, 107374182300)]
        public void PageOffset_is_number_of_results_before_current_page(int pageNumber, int pageSize, long expected)
        {
            var paging = new Paging { PageNumber = pageNumber, PageSize = pageSize };

            Assert.Equal(expected, paging.PageOffset);
        }

        [Fact]
        public void PageOffset_is_0_for_default_page_size()
        {
            var paging = new Paging();

            Assert.Equal(0, paging.PageOffset);
        }

        [Theory]
        [InlineData(1, 0, false)]
        [InlineData(1, 50, false)]
        [InlineData(2, 51, false)]
        [InlineData(3, 150, false)]
        [InlineData(2, 0, true)]
        [InlineData(2, 50, true)]
        [InlineData(4, 150, true)]
        [InlineData(int.MaxValue, int.MaxValue, true)]
        public void IsBeyondLastPage_is_true_only_after_the_last_page_of_results(int pageNumber, int total, bool expected)
        {
            var paging = new Paging { PageNumber = pageNumber, PageSize = 50, Total = total };

            Assert.Equal(expected, paging.IsBeyondLastPage);
        }
    }
}
