namespace Stoolball.Statistics
{
    public interface IStatisticsFilterHumanizer
    {
        string EntitiesMatchingFilter(string entities, string matchingFilter);
        string MatchingUserFilter(StatisticsFilter filter);
        string MatchingDefaultFilter(StatisticsFilter filter);
    }
}