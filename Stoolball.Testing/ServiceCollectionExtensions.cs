using Microsoft.Extensions.DependencyInjection;
using Stoolball.Awards;
using Stoolball.Logging;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Testing.ClubDataProviders;
using Stoolball.Testing.CompetitionDataProviders;
using Stoolball.Testing.Factories;
using Stoolball.Testing.MatchDataProviders;
using Stoolball.Testing.PlayerDataProviders;
using Stoolball.Testing.SchoolDataProviders;
using Stoolball.Testing.TournamentDataProviders;

namespace Stoolball.Testing
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="SeedDataGenerator"/> and everything it depends on, so that it can be resolved from an <see cref="IServiceProvider"/> instead of constructed by hand.
        /// </summary>
        public static IServiceCollection AddSeedDataGenerator(this IServiceCollection services)
        {
            services.AddSingleton(new Randomiser(new Random()));
            services.AddSingleton<IOversHelper, OversHelper>();
            services.AddSingleton<IBowlingFiguresCalculator, BowlingFiguresCalculator>();
            services.AddSingleton<IPlayerIdentityFinder, PlayerIdentityFinder>();
            services.AddSingleton<IMatchFinder, MatchFinder>();
            services.AddSingleton<CompetitionFactory>();
            services.AddSingleton<SeasonFactory>();
            services.AddSingleton<TeamFactory>();
            services.AddSingleton<ClubFactory>();
            services.AddSingleton<TournamentFactory>();
            services.AddSingleton<MatchLocationFactory>();
            services.AddSingleton<SchoolFactory>();
            services.AddSingleton<PlayerFactory>();
            services.AddSingleton<OverSetFactory>();
            services.AddSingleton<OverFactory>();
            services.AddSingleton<UmbracoMemberFactory>();
            services.AddSingleton<CommentFactory>();
            services.AddSingleton(new Award { AwardId = Guid.NewGuid(), AwardName = "Player of the match" });

            services.AddSingleton(sp => new MatchFactory(
                sp.GetRequiredService<Randomiser>(),
                sp.GetRequiredService<Award>(),
                sp.GetRequiredService<OverSetFactory>()));

            // One AddSingleton<TBase> per provider, so SeedDataGenerator can resolve each family as an
            // IEnumerable<TBase> - each provider still gets its own combination of factories/collaborators,
            // it's just DI rather than SeedDataGenerator constructing them inline.
            services.AddSingleton<BaseMatchDataProvider>(sp => new APlayerOnlyWinsAnAwardButHasPlayedOtherMatchesWithADifferentTeam(
                sp.GetRequiredService<Randomiser>(), sp.GetRequiredService<MatchFactory>(), sp.GetRequiredService<IBowlingFiguresCalculator>(), sp.GetRequiredService<Award>()));
            services.AddSingleton<BaseMatchDataProvider>(sp => new APlayerWithTwoIdentitiesOnOneTeamTakesFiveWicketsOnlyWhenBothAreCombined(
                sp.GetRequiredService<Randomiser>(), sp.GetRequiredService<MatchFactory>(), sp.GetRequiredService<IBowlingFiguresCalculator>()));
            services.AddSingleton<BaseMatchDataProvider>(sp => new PlayersOnlyRecordedInOnePlace(
                sp.GetRequiredService<MatchFactory>(), sp.GetRequiredService<TeamFactory>(), sp.GetRequiredService<PlayerFactory>(), sp.GetRequiredService<Award>()));
            services.AddSingleton<BaseMatchDataProvider>(sp => new MatchesInTheFuture(
                sp.GetRequiredService<MatchFactory>(), sp.GetRequiredService<TeamFactory>(), sp.GetRequiredService<OverSetFactory>()));
            services.AddSingleton<BaseMatchDataProvider>(sp => new EveryMatchResultType(sp.GetRequiredService<MatchFactory>()));
            services.AddSingleton<BaseMatchDataProvider>(sp => new FiveWicketHaul(
                sp.GetRequiredService<MatchFactory>(), sp.GetRequiredService<TeamFactory>(), sp.GetRequiredService<PlayerFactory>()));
            services.AddSingleton<BaseMatchDataProvider>(sp => new PlentyOfRunOutsAndCatches(
                sp.GetRequiredService<MatchFactory>(), sp.GetRequiredService<TeamFactory>(), sp.GetRequiredService<PlayerFactory>()));
            services.AddSingleton<BaseMatchDataProvider>(sp => new TeamScoresButNoPlayerData(
                sp.GetRequiredService<Randomiser>(), sp.GetRequiredService<MatchFactory>(), sp.GetRequiredService<TeamFactory>()));
            services.AddSingleton<BaseMatchDataProvider>(sp => new FieldingByMultipleIdentities(
                sp.GetRequiredService<Randomiser>(), sp.GetRequiredService<MatchFactory>(), sp.GetRequiredService<TeamFactory>(), sp.GetRequiredService<PlayerFactory>()));
            services.AddSingleton<BaseMatchDataProvider>(sp => new DifferentTeamsWhereSomeonePlaysOnBothTeams(
                sp.GetRequiredService<Randomiser>(), sp.GetRequiredService<MatchFactory>(), sp.GetRequiredService<TeamFactory>(), sp.GetRequiredService<PlayerFactory>()));
            services.AddSingleton<BaseMatchDataProvider>(sp => new MatchInThePastWithFullDetailsProvider(
                sp.GetRequiredService<MatchFactory>(), sp.GetRequiredService<TeamFactory>(), sp.GetRequiredService<CompetitionFactory>(),
                sp.GetRequiredService<SeasonFactory>(), sp.GetRequiredService<MatchLocationFactory>(), sp.GetRequiredService<CommentFactory>(),
                sp.GetRequiredService<OverFactory>(), sp.GetRequiredService<IBowlingFiguresCalculator>(),
                sp.GetRequiredService<TournamentFactory>(), sp.GetRequiredService<Award>()));

            services.AddSingleton<BaseCompetitionDataProvider>(sp => new CompetitionWithTeamsAndOverSetsInSeasonProvider(
                sp.GetRequiredService<CompetitionFactory>(), sp.GetRequiredService<SeasonFactory>(), sp.GetRequiredService<TeamFactory>(), sp.GetRequiredService<OverSetFactory>()));
            services.AddSingleton<BaseCompetitionDataProvider>(sp => new CompetitionWithOneSeasonDataProvider(
                sp.GetRequiredService<CompetitionFactory>(), sp.GetRequiredService<SeasonFactory>()));

            services.AddSingleton<BasePlayerDataProvider>(sp => new PlayersLinkedToMembersProvider(
                sp.GetRequiredService<TeamFactory>(), sp.GetRequiredService<PlayerFactory>()));
            services.AddSingleton<BasePlayerDataProvider>(sp => new PlayersNotLinkedToMembersProvider(
                sp.GetRequiredService<TeamFactory>(), sp.GetRequiredService<PlayerFactory>()));
            services.AddSingleton<BasePlayerDataProvider>(sp => new PlayersLinkedToMembersOnSameTeamAsPlayersNotLinkedToMembersProvider(
                sp.GetRequiredService<TeamFactory>(), sp.GetRequiredService<PlayerFactory>()));

            services.AddSingleton<BaseSchoolDataProvider>(sp => new SchoolDataProvider(
                sp.GetRequiredService<SchoolFactory>(), sp.GetRequiredService<TeamFactory>(), sp.GetRequiredService<MatchLocationFactory>()));

            services.AddSingleton<BaseTournamentDataProvider>(sp => new TournamentWithFullDetailsProvider(
                sp.GetRequiredService<TournamentFactory>(), sp.GetRequiredService<TeamFactory>(), sp.GetRequiredService<PlayerFactory>(),
                sp.GetRequiredService<MatchFactory>(), sp.GetRequiredService<IBowlingFiguresCalculator>(), sp.GetRequiredService<Award>()));
            services.AddSingleton<BaseTournamentDataProvider>(sp => new RandomTournamentDataProvider(
                sp.GetRequiredService<Randomiser>(), sp.GetRequiredService<TournamentFactory>(), sp.GetRequiredService<CommentFactory>()));

            services.AddSingleton<BaseClubDataProvider>(sp => new ClubWithTeamsAndMatchLocationProvider(
                sp.GetRequiredService<ClubFactory>(), sp.GetRequiredService<MatchLocationFactory>()));
            services.AddSingleton<BaseClubDataProvider>(sp => new ClubsWithTeamsDataProvider(
                sp.GetRequiredService<ClubFactory>(), sp.GetRequiredService<TeamFactory>()));

            services.AddSingleton(sp => new SeedDataGenerator(
                sp.GetRequiredService<Randomiser>(),
                sp.GetRequiredService<IBowlingFiguresCalculator>(),
                sp.GetRequiredService<IPlayerIdentityFinder>(),
                sp.GetRequiredService<IMatchFinder>(),
                sp.GetRequiredService<CompetitionFactory>(),
                sp.GetRequiredService<SeasonFactory>(),
                sp.GetRequiredService<TeamFactory>(),
                sp.GetRequiredService<ClubFactory>(),
                sp.GetRequiredService<TournamentFactory>(),
                sp.GetRequiredService<MatchLocationFactory>(),
                sp.GetRequiredService<PlayerFactory>(),
                sp.GetRequiredService<CommentFactory>(),
                sp.GetRequiredService<MatchFactory>(),
                sp.GetRequiredService<IEnumerable<BaseMatchDataProvider>>(),
                sp.GetRequiredService<IEnumerable<BaseCompetitionDataProvider>>(),
                sp.GetRequiredService<IEnumerable<BasePlayerDataProvider>>(),
                sp.GetRequiredService<IEnumerable<BaseSchoolDataProvider>>(),
                sp.GetRequiredService<IEnumerable<BaseTournamentDataProvider>>(),
                sp.GetRequiredService<IEnumerable<BaseClubDataProvider>>()));

            return services;
        }
    }
}
