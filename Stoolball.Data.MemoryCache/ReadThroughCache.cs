using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Stoolball.Data.Abstractions;

namespace Stoolball.Data.MemoryCache
{
    public class ReadThroughCache : IReadThroughCache
    {
        private readonly IMemoryCache _memoryCache;

        public ReadThroughCache(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        /// <inheritdoc/>
        public void InvalidateCache(string key)
        {
            var cancellationKey = CancellationKey(key);
            if (_memoryCache.TryGetValue<CancellationTokenSource>(cancellationKey, out var tokenSource) && tokenSource is not null)
            {
                tokenSource.Cancel();
                _memoryCache.Remove(cancellationKey);
            }
        }

        /// <inheritdoc/>
        public async Task<TResult> ReadThroughCacheAsync<TResult>(Func<Task<TResult>> cacheThis, TimeSpan absoluteCacheExpiration, string key, string dependentKey)
        {
            var cancellationKey = CancellationKey(key);
            var cancellationCacheExpiry = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = absoluteCacheExpiration
            };

            if (!_memoryCache.TryGetValue<CancellationTokenSource>(cancellationKey, out var tokenSource))
            {
                tokenSource = new CancellationTokenSource();
                _memoryCache.Set(cancellationKey, tokenSource, cancellationCacheExpiry);
            }

            if (!_memoryCache.TryGetValue<TResult>(dependentKey, out var data) || data is null)
            {
                data = await cacheThis();
                var dataCacheExpiry = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = absoluteCacheExpiration
                };
                if (tokenSource is not null)
                {
                    dataCacheExpiry.AddExpirationToken(new CancellationChangeToken(tokenSource.Token));
                }
                _memoryCache.Set(dependentKey, data, dataCacheExpiry);

                // Update expiry of the CancellationSource so it hangs around long enough to expire the newly-cached data
                _memoryCache.Set(cancellationKey, tokenSource, cancellationCacheExpiry);
            }
            return data;
        }

        private static string CancellationKey(string cacheKey)
        {
            return cacheKey + "/cancellation";
        }
    }
}
