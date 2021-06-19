namespace Stoolball.MatchLocations
{
    public class MatchLocationFilterQueryStringSerializer : QueryStringSerializerBase, IMatchLocationFilterSerializer
    {
        public string Serialize(MatchLocationFilter filter)
        {
            filter = filter ?? new MatchLocationFilter();
            ResetSerializer();

            Serialize(filter.ExcludeMatchLocationIds, "exclude");
            Serialize(filter.HasActiveTeams, "active");
            Serialize(filter.Query, "q");
            Serialize(filter.SeasonIds, "season");
            Serialize(filter.TeamTypes, "teamtype");
            Serialize(filter.Paging.PageNumber, "page");
            Serialize(filter.Paging.PageSize, "pagesize");

            return Serializer.ToQueryString();
        }
    }
}
