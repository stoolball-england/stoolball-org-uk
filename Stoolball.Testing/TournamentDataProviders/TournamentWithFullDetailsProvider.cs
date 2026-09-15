using Stoolball.Awards;
using Stoolball.Logging;

namespace Stoolball.Testing.TournamentDataProviders
{
    internal class TournamentWithFullDetailsProvider : BaseTournamentDataProvider
    {
        private readonly TournamentFactory _tournamentFactory;
        private readonly TeamFactory _teamFactory;
        private readonly PlayerFactory _playerFactory;
        private readonly MatchFactory _matchFactory;
        private readonly IBowlingFiguresCalculator _bowlingFiguresCalculator;
        private readonly Award _playerOfTheMatchAward;

        internal TournamentWithFullDetailsProvider(TournamentFactory tournamentFactory, TeamFactory teamFactory, PlayerFactory playerFactory, MatchFactory matchFactory,
            IBowlingFiguresCalculator bowlingFiguresCalculator, Award playerOfTheMatchAward)
        {
            _tournamentFactory = tournamentFactory ?? throw new ArgumentNullException(nameof(tournamentFactory));
            _teamFactory = teamFactory ?? throw new ArgumentNullException(nameof(teamFactory));
            _playerFactory = playerFactory ?? throw new ArgumentNullException(nameof(playerFactory));
            _matchFactory = matchFactory ?? throw new ArgumentNullException(nameof(matchFactory));
            _bowlingFiguresCalculator = bowlingFiguresCalculator ?? throw new ArgumentNullException(nameof(bowlingFiguresCalculator));
            _playerOfTheMatchAward = playerOfTheMatchAward ?? throw new ArgumentNullException(nameof(playerOfTheMatchAward));
        }

        internal override IEnumerable<(Tournament Tournament, IEnumerable<Match> Matches)> CreateTournaments(TestData readOnlyTestData)
        {
            var teamWithFullDetails = _teamFactory.CreateTeamWithFullDetails("Team with full details in a tournament");

            var tournament = _tournamentFactory.CreateTournamentInThePastWithFullDetailsExceptMatches();

            tournament.History.AddRange(new[] { new AuditRecord {
                    Action = AuditAction.Create,
                    ActorName = nameof(TournamentWithFullDetailsProvider),
                    AuditDate = DateTimeOffset.UtcNow.AccurateToTheMinute().AddMonths(-2),
                    EntityUri = tournament.EntityUri
                }, new AuditRecord {
                    Action = AuditAction.Update,
                    ActorName = nameof(TournamentWithFullDetailsProvider),
                    AuditDate = DateTimeOffset.UtcNow.AccurateToTheMinute().AddDays(-7),
                    EntityUri = tournament.EntityUri
                } });

            var transientTeamForTournament = _teamFactory.CreateFaker().Generate();
            transientTeamForTournament.TeamType = TeamType.Transient;
            transientTeamForTournament.TeamRoute = tournament.TournamentRoute + transientTeamForTournament.TeamRoute;

            tournament.Teams.AddRange([
                new TeamInTournament
                {
                    TournamentTeamId = Guid.NewGuid(),
                    Team = teamWithFullDetails,
                    TeamRole = TournamentTeamRole.Confirmed
                },
                new TeamInTournament{
                    TournamentTeamId = Guid.NewGuid(),
                    Team = transientTeamForTournament,
                    TeamRole = TournamentTeamRole.Confirmed
                }
            ]);

            var matches = new List<Match>();
            var matchOrderInTournament = 1;
            foreach (var teamInTournament in tournament.Teams)
            {
                // Create a tournament match where the fully-detailed team plays everyone including themselves.
                // teamWithFullDetails is created fresh above, so it never already has player identities to reuse.
                var teamAPlayers = _playerFactory.CreatePlayerIdentityFaker(teamWithFullDetails).Generate(11);
                var teamBPlayers = readOnlyTestData.PlayerIdentities.Where(pi => pi.Team!.TeamId == teamInTournament.Team!.TeamId).ToList();
                if (!teamBPlayers.Any()) { teamBPlayers = _playerFactory.CreatePlayerIdentityFaker(teamInTournament.Team!).Generate(11); }

                var matchInTournament = _matchFactory.CreateMatchBetween(
                    teamWithFullDetails, teamAPlayers,
                    teamInTournament.Team!, teamBPlayers,
                    true, readOnlyTestData, nameof(TournamentWithFullDetailsProvider) + "TournamentMatch");
                matchInTournament.Tournament = tournament;
                matchInTournament.OrderInTournament = matchOrderInTournament;
                matchInTournament.StartTime = tournament.StartTime.AddMinutes((matchOrderInTournament - 1) * 45);
                matchInTournament.Season = null;
                matchInTournament.MatchLocation = tournament.TournamentLocation;
                matchInTournament.PlayersPerTeam = tournament.PlayersPerTeam;

                // Make sure the team on the other side of the match (which may be of any TeamType represented in this tournament,
                // e.g. Regular or Transient) has a player who won an award, and has full bowling statistics recorded against them
                // - overs bowled, a credited wicket, and bowling figures. Without this, MatchFactory's own random chances for
                // awards and bowling data mean it's pure luck whether any given team ends up with match data that's complete
                // enough for tests like FindTeamWithMatchData in UpdateTeamsTests to find.
                if (!matchInTournament.Awards.Any(aw => aw.PlayerIdentity?.Team?.TeamId == teamInTournament.Team!.TeamId))
                {
                    matchInTournament.Awards.Add(new MatchAward
                    {
                        AwardedToId = Guid.NewGuid(),
                        Award = _playerOfTheMatchAward,
                        PlayerIdentity = teamBPlayers.First(),
                        Reason = "Outstanding performance in the match"
                    });
                }

                var inningsBowledByTeamB = matchInTournament.MatchInnings.First(mi => mi.BowlingTeam?.Team?.TeamId == teamInTournament.Team!.TeamId);
                if (!inningsBowledByTeamB.OversBowled.Any(o => o.Bowler?.Team?.TeamId == teamInTournament.Team!.TeamId))
                {
                    inningsBowledByTeamB.OversBowled.Add(new Over
                    {
                        OverId = Guid.NewGuid(),
                        OverSet = inningsBowledByTeamB.OverSets.First(),
                        Bowler = teamBPlayers.First(),
                        OverNumber = inningsBowledByTeamB.OversBowled.Count + 1,
                        BallsBowled = 8
                    });
                }
                if (!inningsBowledByTeamB.PlayerInnings.Any(pi => pi.Bowler?.Team?.TeamId == teamInTournament.Team!.TeamId || pi.DismissedBy?.Team?.TeamId == teamInTournament.Team!.TeamId))
                {
                    var dismissedPlayerInnings = inningsBowledByTeamB.PlayerInnings.First();
                    dismissedPlayerInnings.DismissalType = DismissalType.Bowled;
                    dismissedPlayerInnings.Bowler = teamBPlayers.First();
                    dismissedPlayerInnings.DismissedBy = null;
                }

                foreach (var matchInnings in matchInTournament.MatchInnings)
                {
                    matchInnings.BowlingFigures = _bowlingFiguresCalculator.CalculateBowlingFigures(matchInnings);
                }

                matchOrderInTournament++;
                matches.Add(matchInTournament);
                tournament.Matches.Add(new MatchInTournament
                {
                    MatchId = matchInTournament.MatchId,
                    MatchName = matchInTournament.MatchName,
                    Teams = new List<TeamInTournament> { tournament.Teams.Single(x => x.Team?.TeamId == teamWithFullDetails.TeamId), teamInTournament }
                });
            }

            return [(tournament, matches)];
        }
    }
}
