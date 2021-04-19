namespace Stoolball.Caching
{
    public interface ICacheOverride
    {
        void OverrideCacheForCurrentMember(string cacheKeyPrefix);
        bool IsCacheOverriddenForCurrentMember(string cacheKeyPrefix);
    }
}