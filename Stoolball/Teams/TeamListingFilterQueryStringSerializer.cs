namespace Stoolball.Teams
{
    public class TeamListingFilterQueryStringSerializer : QueryStringSerializerBase, ITeamListingFilterSerializer
    {
        public string Serialize(TeamListingFilter filter)
        {
            filter = filter ?? new TeamListingFilter();
            ResetSerializer();

            Serialize(filter.ActiveTeams, "active");
            Serialize(filter.CompetitionIds, "competition");
            Serialize(filter.ExcludeTeamIds, "exclude");
            Serialize(filter.Query, "q");
            Serialize(filter.TeamTypes, "teamtype");
            Serialize(filter.Paging.PageNumber, "page");
            Serialize(filter.Paging.PageSize, "pagesize");

            return Serializer.ToQueryString();
        }
    }
}
