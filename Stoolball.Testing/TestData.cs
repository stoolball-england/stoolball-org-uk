using System;
using System.Collections.Generic;
using System.Linq;
using Stoolball.Clubs;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Schools;
using Stoolball.Statistics;
using Stoolball.Teams;

namespace Stoolball.Testing
{
    public class TestData
    {
        private static Random _random = new();
        internal List<MatchAward> Awards { get; set; } = new();
        internal Club? ClubWithMinimalDetails { get; set; }
        internal Club? ClubWithTeamsAndMatchLocation { get; set; }
        internal List<Club> Clubs { get; set; } = new();
        internal Player? BowlerWithMultipleIdentities { get; set; }
        internal List<Player> Players { get; set; } = new();
        internal List<PlayerInnings> PlayerInnings { get; set; } = new();

        internal List<BowlingFigures> BowlingFigures { get; set; } = new();
        internal List<PlayerIdentity> PlayerIdentities { get; set; } = new();
        internal List<Match> Matches { get; set; } = new();
        internal List<MatchInnings> MatchInnings { get; set; } = new();
        internal Team? TeamWithMinimalDetails { get; set; }
        internal Team? TeamWithFullDetails { get; set; }
        internal List<Team> Teams { get; set; } = new();
        internal List<TeamListing> TeamListings { get; set; } = new();
        internal MatchLocation? MatchLocationWithMinimalDetails { get; set; }
        internal MatchLocation? MatchLocationForClub { get; set; }
        internal List<MatchLocation> MatchLocations { get; set; } = new();
        internal Competition? CompetitionWithMinimalDetails { get; set; }
        internal List<Competition> Competitions { get; set; } = new();
        internal List<Player> PlayersWithMultipleIdentities { get; set; } = new();
        internal MatchLocation? MatchLocationWithFullDetails { get; set; }
        internal Competition? CompetitionWithFullDetails { get; set; }
        internal List<Season> Seasons { get; set; } = new();
        internal Season? SeasonWithMinimalDetails { get; set; }
        internal Season? SeasonWithFullDetails { get; set; }
        internal List<(Guid memberKey, string memberName)> Members { get; set; } = new();
        internal Match? MatchInThePastWithMinimalDetails { get; set; }
        internal Match? MatchInTheFutureWithMinimalDetails { get; set; }
        internal Match? MatchInThePastWithFullDetails { get; set; }
        internal Match? MatchInThePastWithFullDetailsAndTournament { get; set; }
        internal Tournament? TournamentInThePastWithFullDetails { get; set; }
        internal List<Tournament> Tournaments { get; set; } = new();
        internal Tournament? TournamentInThePastWithMinimalDetails { get; set; }
        internal Tournament? TournamentInTheFutureWithMinimalDetails { get; set; }
        internal List<MatchListing> MatchListings { get; set; } = new();
        internal List<MatchListing> TournamentMatchListings { get; set; } = new();
        internal List<School> Schools { get; set; } = new();

        internal IEnumerable<Match> MatchesThatCouldHavePlayerStatistics()
        {
            return Matches.Where(m => m.MatchType != MatchType.TrainingSession && m.StartTime <= DateTimeOffset.UtcNow);
        }

        internal (PlayerIdentity firstIdentity, PlayerIdentity secondIdentity) AnyTwoIdentitiesFromTheSameTeam()
        {
            PlayerIdentity? firstIdentity = null;
            PlayerIdentity? secondIdentity = null;

            foreach (var identity in PlayerIdentities)
            {
                firstIdentity = identity;
                secondIdentity = PlayerIdentities.FirstOrDefault(x => x.PlayerIdentityId != firstIdentity.PlayerIdentityId && x.Team?.TeamId == firstIdentity.Team?.TeamId);

                if (secondIdentity != null)
                {
                    break;
                }
            }

            return (firstIdentity!, secondIdentity!);
        }

        internal IEnumerable<Player> PlayersWhoHavePlayedAtLeastOneMatch() => Players.Where(p => p.PlayerIdentities.Any(pi => pi.FirstPlayed is not null));
        internal IEnumerable<PlayerIdentity> PlayerIdentitiesWhoHavePlayedAtLeastOneMatch() => PlayerIdentities.Where(pi => pi.FirstPlayed is not null);

        internal Player AnyPlayerWithOnlyOneIdentity(Func<Player, bool>? additionalCriteria = null)
        {
            additionalCriteria = additionalCriteria ?? (x => true);
            return Players.First(x => x.PlayerIdentities.Count == 1 && additionalCriteria(x));
        }

        internal Player AnyPlayerWithMultipleIdentities(Func<Player, bool>? additionalCriteria = null)
        {
            additionalCriteria = additionalCriteria ?? (x => true);
            return Players.First(x => x.PlayerIdentities.Count > 1 && additionalCriteria(x));
        }

        internal Player AnyPlayerLinkedToMember(Func<Player, bool>? additionalCriteria = null)
        {
            additionalCriteria = additionalCriteria ?? (x => true);
            return Players.First(x => x.MemberKey.HasValue && additionalCriteria(x));
        }

        internal Player AnyPlayerNotLinkedToMember(Func<Player, bool>? additionalCriteria = null)
        {
            additionalCriteria = additionalCriteria ?? (x => true);
            return Players.First(x => !x.MemberKey.HasValue && additionalCriteria(x));
        }

        internal Player AnyPlayerLinkedToMemberWithMultipleIdentities(Func<Player, bool>? additionalCriteria = null)
        {
            additionalCriteria = additionalCriteria ?? (x => true);
            return Players.First(x => x.MemberKey.HasValue && x.PlayerIdentities.Count > 1 && additionalCriteria(x));
        }

        internal Player AnyPlayerLinkedToMemberWithOnlyOneIdentity(Func<Player, bool>? additionalCriteria = null)
        {
            additionalCriteria = additionalCriteria ?? (x => true);
            return Players.First(x => x.MemberKey.HasValue && x.PlayerIdentities.Count == 1 && additionalCriteria(x));
        }

        internal Player AnyPlayerNotLinkedToMemberWithOnlyOneIdentity(Func<Player, bool>? additionalCriteria = null)
        {
            additionalCriteria = additionalCriteria ?? (x => true);
            return Players.First(x => !x.MemberKey.HasValue && x.PlayerIdentities.Count == 1 && additionalCriteria(x));
        }

        internal Player AnyPlayerNotLinkedToMemberWithMultipleIdentities(Func<Player, bool>? additionalCriteria = null)
        {
            additionalCriteria = additionalCriteria ?? (x => true);
            return Players.First(x => !x.MemberKey.HasValue && x.PlayerIdentities.Count > 1 && additionalCriteria(x));
        }

        internal (Guid memberKey, string memberName) AnyMemberLinkedToPlayer(Func<(Guid, string), bool>? additionalCriteria = null)
        {
            additionalCriteria = additionalCriteria ?? (x => true);
            return Members.First(x => Players.Any(p => p.MemberKey == x.memberKey) && additionalCriteria(x));
        }

        internal (Guid memberKey, string memberName) AnyMemberLinkedToPlayerWithOnlyOneIdentity(Func<(Guid, string), bool>? additionalCriteria = null)
        {
            additionalCriteria = additionalCriteria ?? (x => true);
            return Members.First(x => Players.Any(p => p.MemberKey == x.memberKey && p.PlayerIdentities.Count == 1) && additionalCriteria(x));
        }

        internal (Guid memberKey, string memberName) AnyMemberLinkedToPlayerWithMultipleIdentities(Func<(Guid, string), bool>? additionalCriteria = null)
        {
            additionalCriteria = additionalCriteria ?? (x => true);
            return Members.First(x => Players.Any(p => p.MemberKey == x.memberKey && p.PlayerIdentities.Count > 1) && additionalCriteria(x));
        }

        internal (Guid memberKey, string memberName) AnyMemberNotLinkedToPlayer(Func<(Guid, string), bool>? additionalCriteria = null)
        {
            additionalCriteria = additionalCriteria ?? (x => true);
            return Members.First(x => !Players.Any(p => p.MemberKey == x.memberKey) && additionalCriteria(x));
        }
        internal (Guid memberKey, string memberName) AnyMemberNotLinkedToPlayerWithOnlyOneIdentity(Func<(Guid, string), bool>? additionalCriteria = null)
        {
            additionalCriteria = additionalCriteria ?? (x => true);
            return Members.First(x => !Players.Any(p => p.MemberKey == x.memberKey && p.PlayerIdentities.Count == 1) && additionalCriteria(x));
        }

        internal (Guid memberKey, string memberName) AnyMemberNotLinkedToPlayerWithMultipleIdentities(Func<(Guid, string), bool>? additionalCriteria = null)
        {
            additionalCriteria = additionalCriteria ?? (x => true);
            return Members.First(x => !Players.Any(p => p.MemberKey == x.memberKey && p.PlayerIdentities.Count > 1) && additionalCriteria(x));
        }
    }
}
