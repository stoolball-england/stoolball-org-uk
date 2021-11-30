using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Moq;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Logging;
using Stoolball.Testing;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Logging
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class SqlServerAuditRepositoryTests
    {
        private readonly SqlServerTestDataFixture _databaseFixture;

        public SqlServerAuditRepositoryTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Create_audit_throws_ArgumentNullException_if_audit_is_null()
        {
            var repo = new SqlServerAuditRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.CreateAudit(null, Mock.Of<IDbTransaction>()).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Create_audit_throws_ArgumentNullException_if_transaction_is_null()
        {
            var repo = new SqlServerAuditRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.CreateAudit(new AuditRecord(), null).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Create_audit_with_minimal_details_succeeds()
        {
            var audit = new AuditRecord
            {
                Action = AuditAction.Update,
                ActorName = "Example actor",
                AuditDate = DateTimeOffset.UtcNow.AccurateToTheMinute(),
                EntityUri = new Uri("https://example.org/example")
            };

            await ActAndAssert(audit).ConfigureAwait(false);
        }

        [Fact]
        public async Task Create_audit_with_full_details_succeeds()
        {
            var audit = new AuditRecord
            {
                Action = AuditAction.Update,
                ActorName = "Example actor",
                AuditDate = DateTimeOffset.UtcNow.AccurateToTheMinute(),
                EntityUri = new Uri("https://example.org/example"),
                MemberKey = Guid.NewGuid(),
                State = "{ state: true }",
                RedactedState = "{ redacted: true }"
            };

            await ActAndAssert(audit).ConfigureAwait(false);
        }

        private async Task ActAndAssert(AuditRecord audit)
        {
            var repo = new SqlServerAuditRepository();

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var createdAudit = await repo.CreateAudit(audit, transaction).ConfigureAwait(false);

                    var auditResult = await connection.QuerySingleOrDefaultAsync<AuditRecord>(
                            $"SELECT AuditId, Action, ActorName, AuditDate, EntityUri, MemberKey, State, RedactedState FROM {Tables.Audit} WHERE AuditId = @AuditId",
                            new { createdAudit.AuditId },
                            transaction
                        ).ConfigureAwait(false);

                    Assert.NotNull(auditResult);
                    Assert.Equal(audit.Action, auditResult.Action);
                    Assert.Equal(audit.ActorName, auditResult.ActorName);
                    Assert.Equal(audit.AuditDate, auditResult.AuditDate);
                    Assert.Equal(audit.EntityUri, auditResult.EntityUri);
                    Assert.Equal(audit.MemberKey, auditResult.MemberKey);
                    Assert.Equal(audit.State, auditResult.State);
                    Assert.Equal(audit.RedactedState, auditResult.RedactedState);

                    transaction.Rollback();
                }
            }
        }

    }
}
