using System;

namespace Stoolball.Web.Filtering
{
    public class FilterViewModel
    {
        public string FilteredItemTypeSingular { get; set; }
        public string FilteredItemTypePlural { get; set; }
        public string FilterDescription { get; set; }
        public DateTimeOffset? from { get; set; }
        public DateTimeOffset? to { get; set; }
    }
}