namespace Stoolball.Umbraco.Data.Competitions
{
    public class CompetitionQuery
    {
        public string Query { get; internal set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}