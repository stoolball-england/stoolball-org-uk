using System;

namespace Stoolball.Web.Matches
{
    public class MatchFilterViewModel
    {
        public string FilterDescription { get; set; }
        public DateTimeOffset? from { get; set; }
        public DateTimeOffset? to { get; set; }
    }
}