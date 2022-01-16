using Stoolball.Web.Filtering;

namespace Stoolball.Web.Matches
{
    public class MatchFilterViewModel : FilterViewModel
    {
        public MatchFilterViewModel()
        {
            FilteredItemTypeSingular = "Match";
            FilteredItemTypePlural = "Matches";
        }
    }
}