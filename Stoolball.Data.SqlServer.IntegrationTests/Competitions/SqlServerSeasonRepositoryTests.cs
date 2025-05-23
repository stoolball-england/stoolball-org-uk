﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;
using AngleSharp.Css.Dom;
using Dapper;
using Ganss.Xss;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Logging;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Competitions
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class SqlServerSeasonRepositoryTests : IDisposable
    {
        private readonly SqlServerTestDataFixture _databaseFixture;
        private readonly TransactionScope _scope;

        public SqlServerSeasonRepositoryTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
            _scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        }

        [Fact]
        public async Task Delete_season_succeeds()
        {
            var sanitizer = new Mock<IHtmlSanitizer>();
            sanitizer.Setup(x => x.AllowedTags).Returns(new HashSet<string>());
            sanitizer.Setup(x => x.AllowedAttributes).Returns(new HashSet<string>());
            sanitizer.Setup(x => x.AllowedCssProperties).Returns(new HashSet<string>());
            sanitizer.Setup(x => x.AllowedAtRules).Returns(new HashSet<CssRuleType>());

            var memberKey = Guid.NewGuid();
            var memberName = "Dee Leeter";

            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(_databaseFixture.TestData.SeasonWithFullDetails)).Returns(new Season { SeasonId = _databaseFixture.TestData.SeasonWithFullDetails!.SeasonId });

            var seasonRepository = new SqlServerSeasonRepository(
                _databaseFixture.ConnectionFactory,
                Mock.Of<IAuditRepository>(),
                Mock.Of<ILogger<SqlServerSeasonRepository>>(),
                sanitizer.Object,
                Mock.Of<IRedirectsRepository>(),
                copier.Object
                );

            await seasonRepository.DeleteSeason(_databaseFixture.TestData.SeasonWithFullDetails, memberKey, memberName).ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<Guid?>($"SELECT SeasonId FROM {Tables.Season} WHERE SeasonId = @SeasonId", new { _databaseFixture.TestData.SeasonWithFullDetails.SeasonId }).ConfigureAwait(false);
                Assert.Null(result);
            }
        }
        public void Dispose() => _scope.Dispose();
    }
}
