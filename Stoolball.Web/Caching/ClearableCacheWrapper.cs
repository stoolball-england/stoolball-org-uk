using System;
using Microsoft.Extensions.Caching.Memory;
using Stoolball.Caching;

namespace Stoolball.Web.Caching
{
    public class ClearableCacheWrapper : IClearableCache
    {
        private readonly IMemoryCache _memoryCache;
        private bool _disposedValue;

        public ClearableCacheWrapper(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }


        public void Remove(object key)
        {
            if (!_disposedValue)
            {
                _memoryCache.Remove(key);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _memoryCache.Dispose();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}