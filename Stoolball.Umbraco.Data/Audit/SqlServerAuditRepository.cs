using System;
using System.Threading.Tasks;
using Stoolball.Audit;
using Umbraco.Core.Logging;
using Umbraco.Core.Scoping;

namespace Stoolball.Umbraco.Data.Audit
{
    /// <summary>
    /// A store of audit records in the Umbraco database for changes to stoolball data
    /// </summary>
    public class SqlServerAuditRepository : IAuditRepository
    {
        private readonly IScopeProvider _scopeProvider;
        private readonly ILogger _logger;

        public SqlServerAuditRepository(IScopeProvider scopeProvider, ILogger logger)
        {
            _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a new audit record
        /// </summary>
        /// <param name="audit">The audit details to record</param>
        public async Task CreateAudit(AuditRecord audit)
        {
            if (audit is null)
            {
                throw new System.ArgumentNullException(nameof(audit));
            }
            using (var scope = _scopeProvider.CreateScope())
            {
                await scope.Database.ExecuteAsync($@"INSERT INTO {Constants.Tables.Audit} 
                ([AuditId], [MemberKey], [ActorName], [Action], [EntityUri], [State], [AuditDate]) 
                VALUES (@0, @1, @2, @3, @4, @5, @6)",
                Guid.NewGuid(),
                audit.MemberKey,
                audit.ActorName,
                audit.Action.ToString(),
                audit.EntityUri.ToString(),
                audit.State,
                audit.AuditDate.UtcDateTime
                ).ConfigureAwait(false);

                scope.Complete();
            }
        }
    }
}
