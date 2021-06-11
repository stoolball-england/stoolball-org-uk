using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;
using Polly.Registry;
using Stoolball.Caching;
using Stoolball.Comments;

namespace Stoolball.Data.Cache
{
    public class CachedCommentsDataSource<T> : ICommentsDataSource<T>
    {
        private readonly IReadOnlyPolicyRegistry<string> _policyRegistry;
        private readonly ICacheableCommentsDataSource<T> _commentsDataSource;

        public CachedCommentsDataSource(IReadOnlyPolicyRegistry<string> policyRegistry, ICacheableCommentsDataSource<T> commentsDataSource)
        {
            _policyRegistry = policyRegistry ?? throw new ArgumentNullException(nameof(policyRegistry));
            _commentsDataSource = commentsDataSource ?? throw new ArgumentNullException(nameof(commentsDataSource));
        }

        public async Task<List<HtmlComment>> ReadComments(Guid entityId)
        {
            var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.CommentsPolicy);
            return await cachePolicy.ExecuteAsync(async context => await _commentsDataSource.ReadComments(entityId).ConfigureAwait(false), new Context(nameof(ReadComments) + entityId));
        }

        public async Task<int> ReadTotalComments(Guid entityId)
        {
            return await _commentsDataSource.ReadTotalComments(entityId).ConfigureAwait(false);
        }
    }
}
