﻿using System.Data;
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
        /// <param name="transaction">The transaction to audit</param>
        Task<AuditRecord> CreateAudit(AuditRecord audit, IDbTransaction transaction);
    }
}