using System;

namespace Stoolball.Navigation
{
    public class Paging
    {
        public Uri PageUrl { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = int.MaxValue;
        public int Total { get; set; }
    }
}
