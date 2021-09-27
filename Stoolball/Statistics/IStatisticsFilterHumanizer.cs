namespace Stoolball.Statistics
{
    public interface IStatisticsFilterHumanizer
    {
        string EntitiesMatchingFilter(string entities, string matchingFilter);
        string MatchingUserFilter(StatisticsFilter filter);
        string MatchingFixedFilter(StatisticsFilter filter);
    }
}