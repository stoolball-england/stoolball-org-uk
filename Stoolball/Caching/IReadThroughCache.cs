using System;
using System.Threading.Tasks;

namespace Stoolball.Caching
{
    public interface IReadThroughCache
    {
        void InvalidateCache(string key);
        Task<TResult> ReadThroughCacheAsync<TResult>(Func<Task<TResult>> cacheThis, TimeSpan absoluteCacheExpiration, string key, string dependentKey);
    }
}