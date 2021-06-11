using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;
using Polly.Registry;
using Stoolball.Caching;
using Stoolball.Competitions;

namespace Stoolball.Data.Cache
{
    public class CachedCompetitionDataSource : ICompetitionDataSource
    {
        private readonly ICacheableCompetitionDataSource _competitionDataSource;
        private readonly IReadOnlyPolicyRegistry<string> _policyRegistry;
        private readonly ICompetitionFilterSerializer _competitionFilterSerializer;
        private readonly ICacheOverride _cacheOverride;
        private bool? _cacheDisabled;

        public CachedCompetitionDataSource(IReadOnlyPolicyRegistry<string> policyRegistry, ICacheableCompetitionDataSource competitionDataSource, ICompetitionFilterSerializer competitionFilterSerializer, ICacheOverride cacheOverride)
        {
            _policyRegistry = policyRegistry ?? throw new ArgumentNullException(nameof(policyRegistry));
            _competitionDataSource = competitionDataSource ?? throw new ArgumentNullException(nameof(competitionDataSource));
            _competitionFilterSerializer = competitionFilterSerializer ?? throw new ArgumentNullException(nameof(competitionFilterSerializer));
            _cacheOverride = cacheOverride ?? throw new ArgumentNullException(nameof(cacheOverride));
        }

        private bool CacheDisabled()
        {
            if (!_cacheDisabled.HasValue)
            {
                _cacheDisabled = _cacheOverride.IsCacheOverriddenForCurrentMember(CacheConstants.CompetitionsPolicyCacheKeyPrefix);
            }
            return _cacheDisabled.Value;
        }

        /// <inheritdoc />
        public async Task<int> ReadTotalCompetitions(CompetitionFilter filter)
        {
            filter = filter ?? new CompetitionFilter();

            if (CacheDisabled())
            {
                return await _competitionDataSource.ReadTotalCompetitions(filter).ConfigureAwait(false);
            }
            else
            {
                var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.CompetitionsPolicy);
                var cacheKey = CacheConstants.CompetitionsPolicyCacheKeyPrefix + nameof(ReadTotalCompetitions) + _competitionFilterSerializer.Serialize(filter);
                return await cachePolicy.ExecuteAsync(async context => await _competitionDataSource.ReadTotalCompetitions(filter).ConfigureAwait(false), new Context(cacheKey));
            }
        }

        /// <inheritdoc />
        public async Task<List<Competition>> ReadCompetitions(CompetitionFilter filter)
        {
            filter = filter ?? new CompetitionFilter();

            if (CacheDisabled())
            {
                return await _competitionDataSource.ReadCompetitions(filter).ConfigureAwait(false);
            }
            else
            {
                var cachePolicy = _policyRegistry.Get<IAsyncPolicy>(CacheConstants.CompetitionsPolicy);
                var cacheKey = CacheConstants.CompetitionsPolicyCacheKeyPrefix + nameof(ReadCompetitions) + _competitionFilterSerializer.Serialize(filter);
                return await cachePolicy.ExecuteAsync(async context => await _competitionDataSource.ReadCompetitions(filter).ConfigureAwait(false), new Context(cacheKey));
            }
        }

        /// <inheritdoc />
        public async Task<Competition> ReadCompetitionByRoute(string route)
        {
            return await _competitionDataSource.ReadCompetitionByRoute(route).ConfigureAwait(false);
        }
    }
}
