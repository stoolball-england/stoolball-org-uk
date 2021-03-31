namespace Stoolball.Matches
{
    public interface IMatchFilterSerializer
    {
        string Serialize(MatchFilter filter);
    }
}