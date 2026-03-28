namespace Stoolball.Testing.Factories
{
    public class TournamentFactory(SeasonFactory _seasonFactory, CommentFactory _commentFactory)
    {
        public Faker<Tournament> CreateFaker()
        {
            return new Faker<Tournament>()
                    .RuleFor(x => x.TournamentId, () => Guid.NewGuid())
                    .RuleFor(x => x.StartTime, () => new DateTimeOffset(2020, 8, 10, 10, 00, 00, TimeSpan.FromHours(1)))
                    .RuleFor(x => x.TournamentName, (faker, tournament) => $"Example tournament {tournament.TournamentId}")
                    .RuleFor(x => x.TournamentRoute, (faker, tournament) => $"/tournaments/{tournament.TournamentName.Kebaberize()}")
                    .RuleFor(x => x.MemberKey, () => Guid.NewGuid());
        }

        public Tournament CreateTournamentInThePastWithFullDetailsExceptMatches(
            Faker<Competition> _competitionFaker,
            Faker<Team> _teamFaker,
            Faker<MatchLocation> _matchLocationFaker,
            Faker<OverSet> _overSetFaker,
            List<UmbracoMember> members)
        {
            var competitions = _competitionFaker.Generate(2);
            var season1 = _seasonFactory.CreateFaker(competitions[0]).Generate();
            var season2 = _seasonFactory.CreateFaker(competitions[1]).Generate();
            season2.FromYear = season1.FromYear;
            season2.UntilYear = season1.UntilYear;

            var tournament = new Tournament
            {
                TournamentId = Guid.NewGuid(),
                StartTime = new DateTimeOffset(2020, 8, 10, 10, 00, 00, TimeSpan.FromHours(1)),
                StartTimeIsKnown = true,
                TournamentName = "Example tournament",
                TournamentRoute = "/tournaments/example-tournament-" + Guid.NewGuid(),
                PlayerType = PlayerType.JuniorGirls,
                PlayersPerTeam = 10,
                QualificationType = TournamentQualificationType.ClosedTournament,
                MaximumTeamsInTournament = 10,
                SpacesInTournament = 8,
                TournamentNotes = "Notes about the tournament",
                MemberKey = Guid.NewGuid(),
                Teams = [
                    new TeamInTournament {
                        TournamentTeamId = Guid.NewGuid(),
                        Team = _teamFaker.Generate()
                    },
                    new TeamInTournament {
                        TournamentTeamId = Guid.NewGuid(),
                        Team = _teamFaker.Generate()
                    },
                    new TeamInTournament {
                        TournamentTeamId = Guid.NewGuid(),
                        Team = _teamFaker.Generate()
                    }
                ],
                TournamentLocation = _matchLocationFaker.Generate(),
                DefaultOverSets = [_overSetFaker.Generate()],
                Seasons = [season1, season2],
                Comments = _commentFactory.CreateFaker(members).Generate(10)
            };
            foreach (var season in tournament.Seasons)
            {
                season.Competition!.Seasons.Add(season);
            }
            return tournament;
        }

    }
}
