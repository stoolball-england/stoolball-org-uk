using System;
using Polly;
using Polly.Caching;
using Polly.Registry;
using Stoolball.Caching;
using Umbraco.Web.Security;

namespace Stoolball.Web.Caching
{
    public class CacheOverride : ICacheOverride
    {
        private readonly IReadOnlyPolicyRegistry<string> _policyRegistry;
        private readonly ISyncCacheProvider _cacheProvider;
        private readonly MembershipHelper _membershipHelper;

        public CacheOverride(IReadOnlyPolicyRegistry<string> policyRegistry, ISyncCacheProvider cacheProvider, MembershipHelper membershipHelper)
        {
            _policyRegistry = policyRegistry ?? throw new ArgumentNullException(nameof(policyRegistry));
            _cacheProvider = cacheProvider ?? throw new System.ArgumentNullException(nameof(cacheProvider));
            _membershipHelper = membershipHelper ?? throw new System.ArgumentNullException(nameof(membershipHelper));
        }

        public void OverrideCacheForCurrentMember(string cacheKeyPrefix)
        {
            var currentMember = _membershipHelper.GetCurrentMember();
            if (currentMember == null) { throw new InvalidOperationException("No member is logged in."); }

            var cachePolicy = _policyRegistry.Get<ISyncPolicy>(CacheConstants.MemberOverridePolicy);
            cachePolicy.Execute(x => true, new Context(CacheConstants.MemberOverridePolicy + cacheKeyPrefix + currentMember.Key));

        }

        public bool IsCacheOverriddenForCurrentMember(string cacheKeyPrefix)
        {
            var currentMember = _membershipHelper.GetCurrentMember();
            if (currentMember == null) { return false; }

            return _cacheProvider.TryGet(CacheConstants.MemberOverridePolicy + cacheKeyPrefix + currentMember.Key).Item1;
        }
    }
}