using System.Threading.Tasks;

namespace Stoolball.Umbraco.Data.Audit
{
    /// <summary>
    /// A store of audit records for changes to stoolball data
    /// </summary>
    public interface IAuditRepository
    {
        Task CreateAudit(AuditRecord audit);
    }
}