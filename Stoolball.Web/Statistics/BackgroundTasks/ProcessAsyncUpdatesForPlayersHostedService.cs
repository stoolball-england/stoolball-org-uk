﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stoolball.Data.Abstractions;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.HostedServices;

namespace Stoolball.Web.Statistics.BackgroundTasks
{
    public class ProcessAsyncUpdatesForPlayersHostedService : RecurringHostedServiceBase
    {
        private readonly IRuntimeState _runtimeState;
        private readonly IPlayerRepository _playerRepository;

        private static TimeSpan HowOftenWeRepeat => TimeSpan.FromMinutes(5);
        private static TimeSpan DelayBeforeWeStart => TimeSpan.FromMinutes(1);

        public ProcessAsyncUpdatesForPlayersHostedService(
            IRuntimeState runtimeState,
            ILogger<ProcessAsyncUpdatesForPlayersHostedService> logger,
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

            await _playerRepository.ProcessAsyncUpdatesForPlayers().ConfigureAwait(false);
        }

    }
}
