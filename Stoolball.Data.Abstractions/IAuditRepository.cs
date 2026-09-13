using System.Data;
using System.Threading.Tasks;
using Stoolball.Logging;

namespace Stoolball.Data.Abstractions
{
    /// <summary>
    /// A store of audit records for changes to stoolball data
    /// </summary>
    public interface IAuditRepository
    {
        /// <summary>
        /// Creates a new audit record
        /// </summary>
        /// <param name="audit">The audit details to record</param>
        /// <param name="connection">The connection to use, which may or may not have an ambient transaction enlisted</param>
        /// <param name="transaction">The transaction to audit, or <c>null</c> if <paramref name="connection"/> is enlisted in an ambient transaction</param>
        Task<AuditRecord> CreateAudit(AuditRecord audit, IDbConnection connection, IDbTransaction? transaction);
    }
}