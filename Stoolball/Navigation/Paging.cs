using System;

namespace Stoolball.Navigation
{
    public class Paging
    {
        public Uri? PageUrl { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = int.MaxValue;
        public int Total { get; set; }

        /// <summary>
        /// Gets the number of results before the first result on the current page. This is a <c>long</c> because a large
        /// <see cref="PageNumber"/> from the querystring multiplied by <see cref="PageSize"/> can overflow an <c>int</c>.
        /// </summary>
        public long PageOffset => (long)PageSize * (Math.Max(PageNumber, 1) - 1);

        /// <summary>
        /// Gets whether the current page is after the last page of results, according to <see cref="Total"/>, which must be set first.
        /// The first page is never beyond the last page, because it is where to report that there are no results.
        /// </summary>
        public bool IsBeyondLastPage => PageNumber > 1 && PageOffset >= Total;
    }
}
