using Stoolball.Navigation;

namespace Stoolball.Listings
{
    public interface IListingsFilter
    {
        string? Query { get; set; }
        Paging Paging { get; set; }
    }
}
