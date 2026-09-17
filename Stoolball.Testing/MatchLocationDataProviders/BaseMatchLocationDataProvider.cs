namespace Stoolball.Testing.MatchLocationDataProviders
{
    internal abstract class BaseMatchLocationDataProvider
    {
        internal abstract IEnumerable<MatchLocation> CreateMatchLocations(TestData readOnlyTestData);
    }
}
