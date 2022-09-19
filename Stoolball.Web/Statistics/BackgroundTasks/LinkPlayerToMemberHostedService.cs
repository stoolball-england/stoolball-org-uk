using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stoolball.Statistics;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.HostedServices;

namespace Stoolball.Web.Statistics.BackgroundTasks
{
    public class LinkPlayerToMemberHostedService : RecurringHostedServiceBase
    {
        private readonly IRuntimeState _runtimeState;
        private readonly IPlayerRepository _playerRepository;

        private static TimeSpan HowOftenWeRepeat => TimeSpan.FromMinutes(5);
        private static TimeSpan DelayBeforeWeStart => TimeSpan.FromMinutes(1);

        public LinkPlayerToMemberHostedService(
            IRuntimeState runtimeState,
            ILogger<LinkPlayerToMemberHostedService> logger,
            IPlayerRepository playerRepository
            )
            : base(logger, HowOftenWeRepeat, DelayBeforeWeStart)
        {
            _runtimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));
            _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
        }

        public async override Task PerformExecuteAsync(object state)
        {
            // Don't do anything if the site is not running.
            if (_runtimeState.Level != RuntimeLevel.Run)
            {
                return;
            }

            await _playerRepository.ProcessAsyncUpdatesForLinkingAndUnlinkingPlayersToMemberAccounts().ConfigureAwait(false);
        }

    }
}
