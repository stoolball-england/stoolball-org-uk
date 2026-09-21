namespace Stoolball.Testing.TeamDataProviders
{
    internal abstract class BaseTeamDataProvider
    {
        internal abstract IEnumerable<(Team team, List<PlayerIdentity> identities)> CreateTeams(TestData readOnlyTestData);
    }
}
