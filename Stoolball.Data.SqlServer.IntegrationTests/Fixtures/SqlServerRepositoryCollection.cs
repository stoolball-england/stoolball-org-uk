using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Fixtures
{
    [CollectionDefinition(IntegrationTestConstants.RepositoryIntegrationTestCollection)]
    public class SqlServerRepositoryCollection : ICollectionFixture<SqlServerRepositoryFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
