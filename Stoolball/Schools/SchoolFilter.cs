using Stoolball.Listings;
using Stoolball.Navigation;

namespace Stoolball.Schools
{
    public class SchoolFilter : IListingsFilter
    {
        public string Query { get; set; }
        public Paging Paging { get; set; } = new Paging();
    }
}