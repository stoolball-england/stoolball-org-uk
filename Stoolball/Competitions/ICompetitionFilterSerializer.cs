namespace Stoolball.Competitions
{
    public interface ICompetitionFilterSerializer
    {
        string Serialize(CompetitionFilter filter);
    }
}