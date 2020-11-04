using System;
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
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly ILogger _logger;

        public SqlServerAuditRepository(IDatabaseConnectionFactory databaseConnectionFactory, ILogger logger)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
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
                throw new ArgumentNullException(nameof(audit));
            }
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    await connection.ExecuteAsync($@"INSERT INTO {Constants.Tables.Audit} 
                        ([AuditId], [MemberKey], [ActorName], [Action], [EntityUri], [State], [AuditDate]) 
                        VALUES (@AuditId, @MemberKey, @ActorName, @Action, @EntityUri, @State, @AuditDate)",
                        new
                        {
                            AuditId = Guid.NewGuid(),
                            audit.MemberKey,
                            audit.ActorName,
                            Action = audit.Action.ToString(),
                            EntityUri = audit.EntityUri.ToString(),
                            audit.State,
                            AuditDate = audit.AuditDate.UtcDateTime
                        },
                        transaction).ConfigureAwait(false);


                    transaction.Commit();
                }
            }
        }
    }
}
