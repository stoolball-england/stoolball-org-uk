namespace Stoolball.Testing.PlayerDataProviders
{
    internal abstract class BaseCompetitionDataProvider
    {
        internal abstract IEnumerable<Competition> CreateCompetitions(TestData readOnlyTestData);
    }
}