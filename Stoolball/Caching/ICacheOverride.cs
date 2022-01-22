using System.Threading.Tasks;

namespace Stoolball.Caching
{
    public interface ICacheOverride
    {
        Task OverrideCacheForCurrentMember(string cacheKeyPrefix);
        Task<bool> IsCacheOverriddenForCurrentMember(string cacheKeyPrefix);
    }
}