namespace Stoolball.MatchLocations
{
    public interface IMatchLocationFilterSerializer
    {
        string Serialize(MatchLocationFilter filter);
    }
}