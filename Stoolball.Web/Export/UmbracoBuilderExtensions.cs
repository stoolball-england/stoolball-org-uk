using Microsoft.Extensions.DependencyInjection;
using Stoolball.Email;
using Umbraco.Cms.Core.DependencyInjection;

namespace Stoolball.Web.Export
{
    public static class UmbracoBuilderExtensions
    {
        public static IUmbracoBuilder AddCsvExport(this IUmbracoBuilder builder)
        {
            builder.Services.AddTransient<IContactDetailsParser, ContactDetailsParser>();
            builder.Services.AddHostedService<OnlySportCsvHostedService>();
            builder.Services.AddHostedService<SpogoCsvHostedService>();
            return builder;
        }
    }
}