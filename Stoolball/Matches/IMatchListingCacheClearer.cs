using System.Threading.Tasks;

namespace Stoolball.Matches
{
    public interface IMatchListingCacheClearer
    {
        Task ClearCacheFor(Tournament tournament);
        Task ClearCacheFor(Match match);
    }
}