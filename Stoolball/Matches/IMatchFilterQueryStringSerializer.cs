namespace Stoolball.Matches
{
    public interface IMatchFilterQueryStringSerializer
    {
        string Serialize(MatchFilter filter);
        string Serialize(MatchFilter filter, MatchFilter defaultFilter);
    }
}