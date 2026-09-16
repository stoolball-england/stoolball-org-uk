namespace Stoolball.Testing.ClubDataProviders
{
    internal abstract class BaseClubDataProvider
    {
        internal abstract IEnumerable<Club> CreateClubs(TestData readOnlyTestData);
    }
}
