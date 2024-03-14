namespace Stoolball.Statistics
{
    public class PlayerFilterQueryStringSerializer : QueryStringSerializerBase, IPlayerFilterSerializer
    {
        public string Serialize(PlayerFilter filter)
        {
            filter = filter ?? new PlayerFilter();
            ResetSerializer();

            Serialize(filter.Query, "q");
            Serialize(filter.PlayerIds, "player");
            Serialize(filter.PlayerIdentityIds, "playeridentity");
            Serialize(filter.ExcludePlayerIdentityIds, "not");
            Serialize(filter.TeamIds, "team");
            Serialize(filter.IncludePlayersAndIdentitiesLinkedToAMember, "member");

            return Serializer.ToQueryString();
        }
    }
}
