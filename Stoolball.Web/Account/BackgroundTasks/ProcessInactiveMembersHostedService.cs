using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;
using Stoolball.Security;
using Stoolball.Web.Statistics.BackgroundTasks;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.HostedServices;

namespace Stoolball.Web.Account.BackgroundTasks
{
    public class ProcessInactiveMembersHostedService : RecurringHostedServiceBase
    {
        private readonly IRuntimeState _runtimeState;
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IMemberService _memberService;
        private readonly IVerificationToken _verificationToken;

        private static TimeSpan HowOftenWeRepeat => TimeSpan.FromHours(4);
        private static TimeSpan DelayBeforeWeStart => TimeSpan.FromMinutes(10);

        public ProcessInactiveMembersHostedService(
            IRuntimeState runtimeState,
            ILogger<ProcessAsyncUpdatesForPlayersHostedService> logger,
            IDatabaseConnectionFactory databaseConnectionFactory,
            IMemberService memberService,
            IVerificationToken verificationToken
            )
            : base(logger, HowOftenWeRepeat, DelayBeforeWeStart)
        {
            _runtimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _memberService = memberService ?? throw new ArgumentNullException(nameof(memberService));
            _verificationToken = verificationToken ?? throw new ArgumentNullException(nameof(verificationToken));
        }

        public async override Task PerformExecuteAsync(object state)
        {
            // Don't do anything if the site is not running.
            if (_runtimeState.Level != RuntimeLevel.Run)
            {
                return;
            }

            IEnumerable<int> inactiveMembers = [];
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                inactiveMembers = await connection.QueryAsync<int>("SELECT TOP 50 nodeId FROM cmsMember WHERE isApproved = 0").ConfigureAwait(false);
            }

            foreach (var memberId in inactiveMembers)
            {
                var member = _memberService.GetById(memberId);
                if (member is null)
                {
                    continue;
                }

                if (_verificationToken.HasExpired(member.GetValue<DateTime>("approvalTokenExpires")) && member.GetValue<int>("totalLogins") == 0)
                {
                    _memberService.Delete(member);
                }
            }
        }

    }
}
