namespace Stoolball.Testing.CompetitionDataProviders
{
    internal abstract class BaseCompetitionDataProvider
    {
        internal abstract IEnumerable<Competition> CreateCompetitions(TestData readOnlyTestData);
    }
}