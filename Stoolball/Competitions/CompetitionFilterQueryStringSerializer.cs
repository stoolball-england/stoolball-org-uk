namespace Stoolball.Competitions
{
    public class CompetitionFilterQueryStringSerializer : QueryStringSerializerBase, ICompetitionFilterSerializer
    {
        public string Serialize(CompetitionFilter filter)
        {
            filter = filter ?? new CompetitionFilter();
            ResetSerializer();

            Serialize(filter.EnableTournaments, "tournaments");
            Serialize(filter.FromYear, "from");
            Serialize(filter.UntilYear, "until");
            Serialize(filter.MatchTypes, "matchtype");
            Serialize(filter.PlayerTypes, "playertype");
            Serialize(filter.Query, "q");
            Serialize(filter.Paging.PageNumber, "page");
            Serialize(filter.Paging.PageSize, "pagesize");

            return Serializer.ToQueryString();
        }
    }
}
