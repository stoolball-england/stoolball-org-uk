using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stoolball.Caching;
using Stoolball.Comments;

namespace Stoolball.Data.Cache
{
    public class CachedCommentsDataSource<T> : ICommentsDataSource<T>
    {
        private readonly IReadThroughCache _readThroughCache;
        private readonly ICacheableCommentsDataSource<T> _commentsDataSource;

        public CachedCommentsDataSource(IReadThroughCache readThroughCache, ICacheableCommentsDataSource<T> commentsDataSource)
        {
            _readThroughCache = readThroughCache ?? throw new ArgumentNullException(nameof(readThroughCache));
            _commentsDataSource = commentsDataSource ?? throw new ArgumentNullException(nameof(commentsDataSource));
        }

        public async Task<List<HtmlComment>> ReadComments(Guid entityId)
        {
            var cacheKey = nameof(ICommentsDataSource<T>) + typeof(T).Name + nameof(ReadComments) + entityId;
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _commentsDataSource.ReadComments(entityId).ConfigureAwait(false), CachePolicy.CommentsExpiration(), cacheKey, cacheKey);
        }

        public async Task<int> ReadTotalComments(Guid entityId)
        {
            var cacheKey = nameof(ICommentsDataSource<T>) + typeof(T).Name + nameof(ReadTotalComments) + entityId;
            return await _readThroughCache.ReadThroughCacheAsync(async () => await _commentsDataSource.ReadTotalComments(entityId).ConfigureAwait(false), CachePolicy.CommentsExpiration(), cacheKey, cacheKey);
        }
    }
}
