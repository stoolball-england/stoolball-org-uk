using System;
using System.Threading.Tasks;
using Polly;
using Polly.Caching;
using Polly.Registry;
using Stoolball.Caching;
using Umbraco.Cms.Core.Security;

namespace Stoolball.Web.Caching
{
    public class CacheOverride : ICacheOverride
    {
        private readonly IReadOnlyPolicyRegistry<string> _policyRegistry;
        private readonly ISyncCacheProvider _cacheProvider;
        private readonly IMemberManager _memberManager;

        public CacheOverride(IReadOnlyPolicyRegistry<string> policyRegistry, ISyncCacheProvider cacheProvider, IMemberManager memberManager)
        {
            _policyRegistry = policyRegistry ?? throw new ArgumentNullException(nameof(policyRegistry));
            _cacheProvider = cacheProvider ?? throw new System.ArgumentNullException(nameof(cacheProvider));
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
        }

        public async Task OverrideCacheForCurrentMember(string cacheKeyPrefix)
        {
            var currentMember = await _memberManager.GetCurrentMemberAsync().ConfigureAwait(false);
            if (currentMember == null) { throw new InvalidOperationException("No member is logged in."); }

            var cachePolicy = _policyRegistry.Get<ISyncPolicy>(CacheConstants.MemberOverridePolicy);
            cachePolicy.Execute(x => true, new Context(CacheConstants.MemberOverridePolicy + cacheKeyPrefix + currentMember.Key));

        }

        public async Task<bool> IsCacheOverriddenForCurrentMember(string cacheKeyPrefix)
        {
            var currentMember = await _memberManager.GetCurrentMemberAsync().ConfigureAwait(false);
            if (currentMember == null) { return false; }

            return _cacheProvider.TryGet(CacheConstants.MemberOverridePolicy + cacheKeyPrefix + currentMember.Key).Item1;
        }
    }
}