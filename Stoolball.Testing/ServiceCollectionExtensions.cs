using Microsoft.Extensions.DependencyInjection;
using Stoolball.Awards;
using Stoolball.Logging;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Testing.Factories;

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
            services.AddSingleton<UmbracoMemberFactory>();
            services.AddSingleton<CommentFactory>();
            services.AddSingleton(new Award { AwardId = Guid.NewGuid(), AwardName = "Player of the match" });

            services.AddSingleton(sp => new SeedDataGenerator(
                sp.GetRequiredService<Randomiser>(),
                sp.GetRequiredService<IOversHelper>(),
                sp.GetRequiredService<IBowlingFiguresCalculator>(),
                sp.GetRequiredService<IPlayerIdentityFinder>(),
                sp.GetRequiredService<IMatchFinder>(),
                sp.GetRequiredService<CompetitionFactory>(),
                sp.GetRequiredService<SeasonFactory>(),
                sp.GetRequiredService<TeamFactory>(),
                sp.GetRequiredService<ClubFactory>(),
                sp.GetRequiredService<TournamentFactory>(),
                sp.GetRequiredService<MatchLocationFactory>(),
                sp.GetRequiredService<SchoolFactory>(),
                sp.GetRequiredService<PlayerFactory>(),
                sp.GetRequiredService<OverSetFactory>(),
                sp.GetRequiredService<UmbracoMemberFactory>(),
                sp.GetRequiredService<CommentFactory>(),
                sp.GetRequiredService<Award>()));

            return services;
        }
    }
}
