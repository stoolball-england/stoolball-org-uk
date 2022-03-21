using Microsoft.AspNetCore.Builder;

namespace Stoolball.Web.Export
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseCsvExport(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CsvMiddleware>();
        }
    }
}
