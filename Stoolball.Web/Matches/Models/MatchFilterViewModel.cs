using Stoolball.Web.Filtering;

namespace Stoolball.Web.Matches.Models
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