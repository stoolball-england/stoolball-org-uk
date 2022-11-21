using System;
using System.Threading.Tasks;

namespace Stoolball.Data.Abstractions
{
    public interface IReadThroughCache
    {
        /// <summary>
        /// Invalidates all variants in a set of cached data matching the shared cache key passed to <see cref="ReadThroughCacheAsync{TResult}(Func{Task{TResult}}, TimeSpan, string, string)"/>
        /// </summary>
        /// <param name="key"></param>
        void InvalidateCache(string key);

        /// <summary>
        /// Reads data from a cache if available, or from the original source if not. When reading from the original source the data is cached for future requests.
        /// </summary>
        /// <typeparam name="TResult">The type of object to cache</typeparam>
        /// <param name="cacheThis">A function which can be executed to get the data from the original source</param>
        /// <param name="absoluteCacheExpiration">The length of time to cache the data for</param>
        /// <param name="key">A shared cache key for all variants of this data which should be invalidated together</param>
        /// <param name="dependentKey">A unique cache key for this variant of the data</param>
        /// <returns>The requested data, either from cache or the original source</returns>
        Task<TResult> ReadThroughCacheAsync<TResult>(Func<Task<TResult>> cacheThis, TimeSpan absoluteCacheExpiration, string key, string dependentKey);
    }
}