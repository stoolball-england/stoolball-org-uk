using System;
using Stoolball.Data.Cache;
using Stoolball.Data.SqlServer;
using Stoolball.Email;
using Stoolball.Statistics;
using Stoolball.Web.Statistics;
using Umbraco.Core;
using Umbraco.Core.Composing;

namespace Stoolball.Web
{
    public class DependencyInjectionComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            if (composition is null)
            {
                throw new ArgumentNullException(nameof(composition));
            }

            // Utility classes
            composition.Register<IBackgroundTaskTracker, MemoryCacheBackgroundTaskTracker>();
            composition.Register<IStatisticsFilterFactory, StatisticsFilterFactory>();
            composition.Register<IStatisticsBreadcrumbBuilder, StatisticsBreadcrumbBuilder>();
            composition.Register<IContactDetailsParser, ContactDetailsParser>();
            composition.Register<IStoolballEntityRouteParser, StoolballEntityRouteParser>();

            // Data sources for stoolball data.
            composition.Register<IPlayerSummaryStatisticsDataSource, CachedPlayerSummaryStatisticsDataSource>();
            composition.Register<ICacheablePlayerSummaryStatisticsDataSource, SqlServerPlayerSummaryStatisticsDataSource>();
            composition.Register<IPlayerPerformanceStatisticsDataSource, CachedPlayerPerformanceStatisticsDataSource>();
            composition.Register<ICacheablePlayerPerformanceStatisticsDataSource, SqlServerPlayerPerformanceStatisticsDataSource>();
            composition.Register<IBestPlayerAverageStatisticsDataSource, CachedBestPlayerAverageStatisticsDataSource>();
            composition.Register<ICacheableBestPlayerAverageStatisticsDataSource, SqlServerBestPlayerAverageStatisticsDataSource>();
        }
    }
}