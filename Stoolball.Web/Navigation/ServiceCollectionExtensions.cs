using Microsoft.Extensions.DependencyInjection;

namespace Stoolball.Web.Navigation
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBreadcrumbBuilders(this IServiceCollection services)
        {
            services.AddTransient<ITeamBreadcrumbBuilder, TeamBreadcrumbBuilder>();
            services.AddTransient<IStatisticsBreadcrumbBuilder, StatisticsBreadcrumbBuilder>();
            return services;
        }
    }
}
