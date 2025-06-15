using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.DependencyInjection;

namespace Stoolball.Web.Account.BackgroundTasks
{
    public static class UmbracoBuilderExtensions
    {
        public static IUmbracoBuilder AddMembersBackgroundTasks(this IUmbracoBuilder builder)
        {
            builder.Services.AddHostedService<ProcessInactiveMembersHostedService>();
            return builder;
        }
    }
}