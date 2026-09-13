using System.Transactions;

namespace Stoolball.Data.SqlServer.IntegrationTests.Redirects
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class SkybrudRedirectsRepositoryTests : IDisposable
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly TransactionScope _scope;

        public SkybrudRedirectsRepositoryTests(SqlServerTestDataFixture databaseFixture)
        {
            _connectionFactory = databaseFixture?.ConnectionFactory ?? throw new ArgumentException($"{nameof(databaseFixture)}.{nameof(databaseFixture.ConnectionFactory)} cannot be null", nameof(databaseFixture));
            _scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        }

        private SkybrudRedirectsRepository CreateRepository()
        {
            return new SkybrudRedirectsRepository();
        }

        [Theory]
        [InlineData("/teams/team-a", "/teams/team-b", null)]
        [InlineData("/teams/team-c", "/teams/team-d", "/players")]
        public async Task Create_redirect_works(string original, string revised, string? suffix)
        {
            var repo = CreateRepository();

            using (var connection = _connectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                var before = DateTime.UtcNow;
                using (var transaction = connection.BeginTransaction())
                {
                    await repo.InsertRedirect(original, revised, suffix, connection, transaction).ConfigureAwait(false);
                    transaction.Commit();
                }
                var after = DateTime.UtcNow;

                var redirect = await connection.QuerySingleAsync<SkybrudRedirectRecord>(@"SELECT [Key], RootKey, Url, QueryString, DestinationType, DestinationId, DestinationKey,
                    DestinationUrl, Created, Updated, IsPermanent, ForwardQueryString, DestinationQuery, DestinationFragment, DestinationCulture
                    FROM SkybrudRedirects WHERE Url = @Url", new { Url = original + suffix }).ConfigureAwait(false);

                Assert.NotEqual(Guid.Empty, redirect.Key);
                Assert.Equal(Guid.Empty, redirect.RootKey);
                Assert.Equal(original + suffix, redirect.Url);
                Assert.Equal(string.Empty, redirect.QueryString);
                Assert.Equal("Url", redirect.DestinationType);
                Assert.Equal(0, redirect.DestinationId);
                Assert.Equal(Guid.Empty, redirect.DestinationKey);
                Assert.Equal(revised + suffix, redirect.DestinationUrl);
                Assert.InRange(redirect.Created, before.AddSeconds(-1), after.AddSeconds(1));
                Assert.InRange(redirect.Updated, before.AddSeconds(-1), after.AddSeconds(1));
                Assert.True(redirect.IsPermanent);
                Assert.True(redirect.ForwardQueryString);
                Assert.Equal(string.Empty, redirect.DestinationQuery);
                Assert.Equal(string.Empty, redirect.DestinationFragment);
                Assert.Equal(string.Empty, redirect.DestinationCulture);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task Create_redirect_throws_ArgumentException_if_originalRoute_is_null_or_empty(string originalRoute)
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.InsertRedirect(originalRoute, "/revised", null, Mock.Of<IDbConnection>(), Mock.Of<IDbTransaction>()).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task Create_redirect_throws_ArgumentException_if_revisedRoute_is_null_or_empty(string revisedRoute)
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.InsertRedirect("/original", revisedRoute, null, Mock.Of<IDbConnection>(), Mock.Of<IDbTransaction>()).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Create_redirect_throws_ArgumentNullException_if_connection_is_null()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.InsertRedirect("/original", "/revised", null, null!, Mock.Of<IDbTransaction>()).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Delete_redirects_by_destination_prefix_throws_ArgumentNullException_if_connection_is_null()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.DeleteRedirectsByDestinationPrefix("/teams/team-to-delete", null!, Mock.Of<IDbTransaction>()).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Delete_redirects_by_destination_prefix_deletes_only_matching_redirects()
        {
            var repo = CreateRepository();
            const string prefix = "/teams/team-to-delete";

            using (var connection = _connectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    // Exact match for the prefix, with nothing after it. This only gets deleted if the
                    // implementation truly treats the trailing % as a wildcard matching zero characters,
                    // rather than requiring something after the prefix.
                    await repo.InsertRedirect("/original-exact-match", prefix, null, connection, transaction).ConfigureAwait(false);

                    // Prefix followed by more of the URL. This is deleted whether the implementation
                    // does a wildcard "starts with" match or a naive substring match.
                    await repo.InsertRedirect("/original-starts-with-prefix", prefix + "/one", null, connection, transaction).ConfigureAwait(false);

                    // Prefix appears, but only after other characters at the start of the URL. This must
                    // be kept - if it were deleted, the query would be matching the prefix anywhere in the
                    // string (e.g. LIKE '%prefix%') rather than anchoring it to the start (LIKE 'prefix%').
                    await repo.InsertRedirect("/original-prefix-not-at-start", "/other" + prefix, null, connection, transaction).ConfigureAwait(false);

                    // Unrelated destination that shares no characters with the prefix. This is the baseline
                    // "definitely not touched" case.
                    await repo.InsertRedirect("/original-to-keep", "/teams/team-to-keep", null, connection, transaction).ConfigureAwait(false);
                    transaction.Commit();
                }

                using (var transaction = connection.BeginTransaction())
                {
                    await repo.DeleteRedirectsByDestinationPrefix(prefix, connection, transaction).ConfigureAwait(false);
                    transaction.Commit();
                }

                var remainingDestinations = await connection.QueryAsync<string>(
                    "SELECT DestinationUrl FROM SkybrudRedirects WHERE Url IN (@exact, @startsWith, @notAtStart, @keep)",
                    new
                    {
                        exact = "/original-exact-match",
                        startsWith = "/original-starts-with-prefix",
                        notAtStart = "/original-prefix-not-at-start",
                        keep = "/original-to-keep"
                    }).ConfigureAwait(false);

                Assert.DoesNotContain(prefix, remainingDestinations);
                Assert.DoesNotContain(prefix + "/one", remainingDestinations);
                Assert.Contains("/other" + prefix, remainingDestinations);
                Assert.Contains("/teams/team-to-keep", remainingDestinations);
            }
        }

        public void Dispose() => _scope.Dispose();

        private record SkybrudRedirectRecord(
            Guid Key,
            Guid RootKey,
            string Url,
            string QueryString,
            string DestinationType,
            int DestinationId,
            Guid DestinationKey,
            string DestinationUrl,
            DateTime Created,
            DateTime Updated,
            bool IsPermanent,
            bool ForwardQueryString,
            string DestinationQuery,
            string DestinationFragment,
            string DestinationCulture);
    }
}
