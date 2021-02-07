using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AngleSharp.Dom.Css;
using Dapper;
using Ganss.XSS;
using Moq;
using Stoolball.Logging;
using Stoolball.Routing;
using Stoolball.Security;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.MatchLocations
{
    [Collection(IntegrationTestConstants.IntegrationTestCollection)]
    public class SqlServerMatchLocationRepositoryTests
    {
        private readonly DatabaseFixture _databaseFixture;

        public SqlServerMatchLocationRepositoryTests(DatabaseFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Delete_match_location_succeeds()
        {
            var sanitizer = new Mock<IHtmlSanitizer>();
            sanitizer.Setup(x => x.AllowedTags).Returns(new HashSet<string>());
            sanitizer.Setup(x => x.AllowedAttributes).Returns(new HashSet<string>());
            sanitizer.Setup(x => x.AllowedCssProperties).Returns(new HashSet<string>());
            sanitizer.Setup(x => x.AllowedAtRules).Returns(new HashSet<CssRuleType>());

            var memberKey = Guid.NewGuid();
            var memberName = "Dee Leeter";

            var repo = new SqlServerMatchLocationRepository(
                _databaseFixture.ConnectionFactory,
                Mock.Of<IAuditRepository>(),
                Mock.Of<ILogger>(),
                Mock.Of<IRouteGenerator>(),
                Mock.Of<IRedirectsRepository>(),
                sanitizer.Object,
                Mock.Of<IDataRedactor>());

            await repo.DeleteMatchLocation(_databaseFixture.MatchLocationWithFullDetailsForDelete, memberKey, memberName).ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<Guid?>($"SELECT MatchLocationId FROM {Tables.MatchLocation} WHERE MatchLocationId = @MatchLocationId", _databaseFixture.MatchLocationWithFullDetailsForDelete).ConfigureAwait(false);
                Assert.Null(result);
            }

            _databaseFixture.MatchLocations.Remove(_databaseFixture.MatchLocationWithFullDetailsForDelete);
            foreach (var team in _databaseFixture.MatchLocationWithFullDetailsForDelete.Teams)
            {
                team.MatchLocations.Remove(_databaseFixture.MatchLocationWithFullDetailsForDelete);
            }
        }
    }
}
