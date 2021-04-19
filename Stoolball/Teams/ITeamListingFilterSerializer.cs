namespace Stoolball.Teams
{
    public interface ITeamListingFilterSerializer
    {
        string Serialize(TeamListingFilter filter);
    }
}