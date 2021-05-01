using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AngleSharp.Css.Dom;
using Dapper;
using Ganss.XSS;
using Moq;
using Stoolball.Logging;
using Stoolball.Routing;
using Stoolball.Security;
using Stoolball.Teams;
using Xunit;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer.IntegrationTests.Teams
{
    [Collection(IntegrationTestConstants.RepositoryIntegrationTestCollection)]
    public class SqlServerTeamRepositoryTests
    {
        private readonly SqlServerRepositoryFixture _databaseFixture;

        public SqlServerTeamRepositoryTests(SqlServerRepositoryFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Create_team_succeeds()
        {
            var team = new Team
            {
                TeamName = "New team " + Guid.NewGuid(),
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = "Test group"
            };

            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateRoute("/teams", team.TeamName, NoiseWords.TeamRoute)).Returns("/teams/" + Guid.NewGuid());

            var sanitiser = new Mock<IHtmlSanitizer>();
            sanitiser.Setup(x => x.AllowedTags).Returns(new HashSet<string>());
            sanitiser.Setup(x => x.AllowedAttributes).Returns(new HashSet<string>());
            sanitiser.Setup(x => x.AllowedCssProperties).Returns(new HashSet<string>());
            sanitiser.Setup(x => x.AllowedAtRules).Returns(new HashSet<CssRuleType>());

            var groupHelper = new Mock<IMemberGroupHelper>();
            groupHelper.Setup(x => x.CreateOrFindGroup("team", team.TeamName, NoiseWords.TeamRoute)).Returns(new SecurityGroup { Key = Guid.NewGuid(), Name = "Group name" });

            var repo = new SqlServerTeamRepository(_databaseFixture.ConnectionFactory, Mock.Of<IAuditRepository>(), Mock.Of<ILogger>(), routeGenerator.Object, Mock.Of<IRedirectsRepository>(),
                groupHelper.Object, sanitiser.Object, Mock.Of<IDataRedactor>());
            var memberKey = Guid.NewGuid();
            var memberUserName = "example@example.org";
            var memberName = "Person 1";

            var created = await repo.CreateTeam(team, memberKey, memberUserName, memberName).ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<Team>($"SELECT TeamId FROM {Tables.Team} WHERE TeamId = @TeamId", new { created.TeamId }).ConfigureAwait(false);
                Assert.NotNull(result);
            }
        }

        [Fact]
        public async Task Delete_team_succeeds()
        {
            var sanitizer = new Mock<IHtmlSanitizer>();
            sanitizer.Setup(x => x.AllowedTags).Returns(new HashSet<string>());
            sanitizer.Setup(x => x.AllowedAttributes).Returns(new HashSet<string>());
            sanitizer.Setup(x => x.AllowedCssProperties).Returns(new HashSet<string>());
            sanitizer.Setup(x => x.AllowedAtRules).Returns(new HashSet<CssRuleType>());

            var memberKey = Guid.NewGuid();
            var memberName = "Dee Leeter";

            var repo = new SqlServerTeamRepository(
                _databaseFixture.ConnectionFactory,
                Mock.Of<IAuditRepository>(),
                Mock.Of<ILogger>(),
                Mock.Of<IRouteGenerator>(),
                Mock.Of<IRedirectsRepository>(),
                Mock.Of<IMemberGroupHelper>(),
                sanitizer.Object,
                Mock.Of<IDataRedactor>());

            await repo.DeleteTeam(_databaseFixture.TeamWithFullDetailsForDelete, memberKey, memberName).ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<Guid?>($"SELECT TeamId FROM {Tables.Team} WHERE TeamId = @TeamId", _databaseFixture.TeamWithFullDetailsForDelete).ConfigureAwait(false);
                Assert.Null(result);
            }
        }
    }
}
