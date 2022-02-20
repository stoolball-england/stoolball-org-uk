using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Caching;
using Polly.Caching.Memory;
using Polly.Registry;
using Stoolball.Caching;
using Stoolball.Clubs;
using Stoolball.Competitions;
using Stoolball.Data.Cache;
using Stoolball.Data.SqlServer;
using Stoolball.Dates;
using Stoolball.Email;
using Stoolball.Html;
using Stoolball.Listings;
using Stoolball.Logging;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Routing;
using Stoolball.Security;
using Stoolball.SocialMedia;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Web.Account;
using Stoolball.Web.Caching;
using Stoolball.Web.Clubs;
using Stoolball.Web.Competitions;
using Stoolball.Web.Competitions.Models;
using Stoolball.Web.Configuration;
using Stoolball.Web.Forms;
using Stoolball.Web.Logging;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace Stoolball.Web
{
    public class Startup
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup" /> class.
        /// </summary>
        /// <param name="webHostEnvironment">The web hosting environment.</param>
        /// <param name="config">The configuration.</param>
        /// <remarks>
        /// Only a few services are possible to be injected here https://github.com/dotnet/aspnetcore/issues/9337
        /// </remarks>
        public Startup(IWebHostEnvironment webHostEnvironment, IConfiguration config)
        {
            _env = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Configures the services.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <remarks>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        /// </remarks>
        public void ConfigureServices(IServiceCollection services)
        {
#pragma warning disable IDE0022 // Use expression body for methods
            services.AddUmbraco(_env, _config)
                .AddBackOffice()
                .AddWebsite()
                .AddComposers()
                .AddNotificationHandler<MemberDeletingNotification, DisableMemberDeleteNotificationHandler>()
                .Build();
#pragma warning restore IDE0022 // Use expression body for methods

            // Utility classes
            services.AddTransient<IApiKeyProvider, ConfigApiKeyProvider>();
            services.AddTransient<IBowlingFiguresCalculator, BowlingFiguresCalculator>();
            services.AddTransient<IDataRedactor, DataRedactor>();
            services.AddTransient<IDateTimeFormatter, DateTimeFormatter>();
            services.AddTransient<IEmailProtector, EmailProtector>();
            services.AddTransient<IEmailFormatter, EmailFormatter>();
            services.AddTransient<IHtmlFormatter, Stoolball.Html.HtmlFormatter>();
            services.AddTransient<Ganss.XSS.IHtmlSanitizer, Ganss.XSS.HtmlSanitizer>();
            services.AddTransient<Stoolball.Html.IHtmlSanitizer, Stoolball.Html.HtmlSanitizer>();
            services.AddTransient(typeof(Stoolball.Logging.ILogger<>), typeof(LogWrapper<>));
            services.AddTransient<IOversHelper, OversHelper>();
            services.AddTransient<ISeasonEstimator, SeasonEstimator>();
            services.AddTransient<ISocialMediaAccountFormatter, SocialMediaAccountFormatter>();
            services.AddTransient<IStoolballEntityCopier, StoolballEntityCopier>();
            services.AddTransient<IUmbracoFormsLabeller, UmbracoFormsLabeller>();
            services.AddTransient<IUrlFormatter, UrlFormatter>();
            services.AddTransient<IVerificationToken, VerificationToken>();
            services.AddTransient<IYouTubeUrlNormaliser, YouTubeUrlNormaliser>();

            // Data filters
            services.AddTransient<ICompetitionFilterSerializer, CompetitionFilterQueryStringSerializer>();
            services.AddTransient<IMatchFilterFactory, MatchFilterFactory>();
            services.AddTransient<IMatchFilterHumanizer, MatchFilterHumanizer>();
            services.AddTransient<IMatchFilterQueryStringParser, MatchFilterQueryStringParser>();
            services.AddTransient<IMatchFilterQueryStringSerializer, MatchFilterQueryStringSerializer>();
            services.AddTransient<IPlayerFilterSerializer, PlayerFilterQueryStringSerializer>();
            services.AddTransient<IStatisticsFilterHumanizer, StatisticsFilterHumanizer>();
            services.AddTransient<IStatisticsFilterQueryStringParser, StatisticsFilterQueryStringParser>();
            services.AddTransient<IStatisticsFilterQueryStringSerializer, StatisticsFilterQueryStringSerializer>();
            services.AddTransient<IStatisticsQueryBuilder, StatisticsQueryBuilder>();

            // Authentication
            services.AddTransient<ICreateMemberExecuter, CreateMemberExecuter>();

            // Data sources
            services.AddTransient<IDatabaseConnectionFactory, UmbracoDatabaseConnectionFactory>();
            services.AddTransient<IRouteNormaliser, RouteNormaliser>();
            services.AddTransient<ICacheOverride, CacheOverride>();

            services.AddTransient<IClubDataSource, SqlServerClubDataSource>();
            services.AddTransient<ICompetitionDataSource, CachedCompetitionDataSource>();
            services.AddTransient<ICacheableCompetitionDataSource, SqlServerCompetitionDataSource>();
            services.AddTransient<IMatchListingDataSource, CachedMatchListingDataSource>();
            services.AddTransient<ICacheableMatchListingDataSource, SqlServerMatchListingDataSource>();
            services.AddTransient<IMatchLocationFilterSerializer, MatchLocationFilterQueryStringSerializer>();
            services.AddTransient<IMatchLocationDataSource, CachedMatchLocationDataSource>();
            services.AddTransient<ICacheableMatchLocationDataSource, SqlServerMatchLocationDataSource>();
            services.AddTransient<IPlayerDataSource, CachedPlayerDataSource>();
            services.AddTransient<ICacheablePlayerDataSource, SqlServerPlayerDataSource>();
            services.AddTransient<ISeasonDataSource, SqlServerSeasonDataSource>();
            services.AddTransient<ITeamDataSource, SqlServerTeamDataSource>();

            // Statistics data sources
            services.AddTransient<IBestPerformanceInAMatchStatisticsDataSource, CachedBestPerformanceInAMatchStatisticsDataSource>();
            services.AddTransient<ICacheableBestPerformanceInAMatchStatisticsDataSource, SqlServerBestPerformanceInAMatchStatisticsDataSource>();
            services.AddTransient<IBestPlayerTotalStatisticsDataSource, CachedBestPlayerTotalStatisticsDataSource>();
            services.AddTransient<ICacheableBestPlayerTotalStatisticsDataSource, SqlServerBestPlayerTotalStatisticsDataSource>();
            services.AddTransient<IInningsStatisticsDataSource, CachedInningsStatisticsDataSource>();
            services.AddTransient<ICacheableInningsStatisticsDataSource, SqlServerInningsStatisticsDataSource>();

            // Caching with Polly
            services.AddSingleton<IMemoryCache, MemoryCache>();
            services.AddSingleton<IOptions<MemoryCacheOptions>, MemoryCacheOptions>();
            services.AddSingleton<IAsyncCacheProvider, MemoryCacheProvider>();
            services.AddSingleton<ISyncCacheProvider, MemoryCacheProvider>();
            services.AddSingleton<IReadOnlyPolicyRegistry<string>>((serviceProvider) =>
            {
                var registry = new PolicyRegistry();
                var asyncMemoryCacheProvider = serviceProvider.GetService<IAsyncCacheProvider>();
                var logger = serviceProvider.GetService<Microsoft.Extensions.Logging.ILogger>();
                var cachePolicy = Policy.CacheAsync(asyncMemoryCacheProvider, TimeSpan.FromMinutes(120), (context, key, ex) =>
                {
                    logger!.LogError(ex, "Cache provider for key {key}, threw exception: {ex}.", key, ex.Message);
                });

                var syncMemoryCacheProvider = serviceProvider.GetService<ISyncCacheProvider>();
                var slidingPolicy = Policy.Cache(syncMemoryCacheProvider, new SlidingTtl(TimeSpan.FromMinutes(120)), (context, key, ex) =>
                {
                    logger!.LogError(ex, "Cache provider for key {key}, threw exception: {ex}.", key, ex.Message);
                });

                registry.Add(CacheConstants.StatisticsPolicy, cachePolicy);
                registry.Add(CacheConstants.MatchesPolicy, cachePolicy);
                registry.Add(CacheConstants.CommentsPolicy, cachePolicy);
                registry.Add(CacheConstants.TeamsPolicy, cachePolicy);
                registry.Add(CacheConstants.CompetitionsPolicy, cachePolicy);
                registry.Add(CacheConstants.MatchLocationsPolicy, cachePolicy);
                registry.Add(CacheConstants.MemberOverridePolicy, slidingPolicy);
                return registry;

            });

            // Repositories
            services.AddTransient<IAuditRepository, SqlServerAuditRepository>();
            services.AddTransient<IClubRepository, SqlServerClubRepository>();
            services.AddTransient<ICompetitionRepository, SqlServerCompetitionRepository>();
            services.AddTransient<IRedirectsRepository, SkybrudRedirectsRepository>();
            services.AddTransient<ISeasonRepository, SqlServerSeasonRepository>();

            // Security checks
            services.AddTransient<IAuthorizationPolicy<Club>, ClubAuthorizationPolicy>();
            services.AddTransient<IAuthorizationPolicy<Competition>, CompetitionAuthorizationPolicy>();
            services.AddScoped<DelegatedContentSecurityPolicyAttribute>();

            // Routing controllers for stoolball data pages.
            services.AddTransient<IRouteGenerator, RouteGenerator>();
            services.AddTransient<IStoolballRouteParser, StoolballRouteParser>();
            services.AddTransient<IStoolballRouteTypeMapper, StoolballRouteTypeMapper>();
            services.AddTransient<IStoolballRouterController, StoolballRouterController>();
            services.AddTransient<IPostSaveRedirector, PostSaveRedirector>();

            // Actual controllers. Register the concrete class since it'll never need to 
            // be injected anywhere except the one place where it's serving a page of content.
            services.AddTransient<ClubController>();
            services.AddTransient<ClubActionsController>();
            services.AddTransient<ClubStatisticsController>();
            services.AddTransient<MatchesForClubController>();
            services.AddTransient<CreateClubController>();
            services.AddTransient<EditClubController>();
            services.AddTransient<DeleteClubController>();

            services.AddTransient<CompetitionsController>();
            services.AddTransient<CompetitionController>();
            services.AddTransient<CompetitionActionsController>();
            services.AddTransient<CompetitionStatisticsController>();
            services.AddTransient<CreateCompetitionController>();
            services.AddTransient<EditCompetitionController>();
            services.AddTransient<DeleteCompetitionController>();

            services.AddTransient<SeasonController>();
            services.AddTransient<SeasonResultsTableController>();
            services.AddTransient<SeasonStatisticsController>();
            services.AddTransient<SeasonActionsController>();
            services.AddTransient<SeasonMapController>();
            services.AddTransient<MatchesForSeasonController>();

            // Listings pages
            services.AddTransient<IListingsModelBuilder<Competition, CompetitionFilter, CompetitionsViewModel>, ListingsModelBuilder<Competition, CompetitionFilter, CompetitionsViewModel>>();
        }

        /// <summary>
        /// Configures the application.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="env">The web hosting environment.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            // Add Cache-Control header to cache static files for 1 year
            app.Use(async (context, next) =>
            {
                string path = context.Request.Path;

                if (path.StartsWith("/umbraco/") == false)
                {
                    if (new List<string> { ".css", ".js", ".svg", ".gif", ".png", ".jpg", ".ico", ".woff", ".woff2" }.Contains(path.GetFileExtension().ToLowerInvariant()))
                    {
                        context.Response.Headers.Add("Cache-Control", "public, max-age=31536000");
                    }
                }

                await next();
            });

            app.UseUmbraco()
                .WithMiddleware(u =>
                {
                    u.UseBackOffice();
                    u.UseWebsite();
                })
                .WithEndpoints(u =>
                {
                    u.UseInstallerEndpoints();
                    u.UseBackOfficeEndpoints();
                    u.UseWebsiteEndpoints();
                });
        }
    }
}
