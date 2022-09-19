using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.DependencyInjection;

namespace Stoolball.Web.Statistics.BackgroundTasks
{
    public static class UmbracoBuilderExtensions
    {
        public static IUmbracoBuilder AddStatisticsBackgroundTasks(this IUmbracoBuilder builder)
        {
            builder.Services.AddHostedService<LinkPlayerToMemberHostedService>();
            return builder;
        }
    }
}