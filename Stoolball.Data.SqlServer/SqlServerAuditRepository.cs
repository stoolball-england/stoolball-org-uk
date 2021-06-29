using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Logging;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// A store of audit records in the Umbraco database for changes to stoolball data
    /// </summary>
    public class SqlServerAuditRepository : IAuditRepository
    {
        /// <summary>
        /// Creates a new audit record
        /// </summary>
        /// <param name="audit">The audit details to record</param>
        /// <param name="transaction">The transaction to audit</param>
        public async Task<AuditRecord> CreateAudit(AuditRecord audit, IDbTransaction transaction)
        {
            if (audit is null)
            {
                throw new ArgumentNullException(nameof(audit));
            }

            if (transaction is null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            audit.AuditId = Guid.NewGuid();
            await transaction.Connection.ExecuteAsync($@"INSERT INTO {Tables.Audit} 
                        ([AuditId], [MemberKey], [ActorName], [Action], [EntityUri], [State], [RedactedState], [AuditDate]) 
                        VALUES (@AuditId, @MemberKey, @ActorName, @Action, @EntityUri, @State, @RedactedState, @AuditDate)",
                new
                {
                    audit.AuditId,
                    audit.MemberKey,
                    audit.ActorName,
                    Action = audit.Action.ToString(),
                    EntityUri = audit.EntityUri.ToString(),
                    audit.State,
                    audit.RedactedState,
                    AuditDate = audit.AuditDate.UtcDateTime
                },
                transaction).ConfigureAwait(false);

            return audit;
        }
    }
}
