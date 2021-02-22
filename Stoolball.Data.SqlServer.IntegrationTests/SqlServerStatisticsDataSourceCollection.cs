using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests
{
    [CollectionDefinition(IntegrationTestConstants.StatisticsDataSourceIntegrationTestCollection)]
    public class SqlServerStatisticsDataSourceCollection : ICollectionFixture<SqlServerStatisticsDataSourceFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
