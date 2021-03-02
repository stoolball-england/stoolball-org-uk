using System.Collections.Generic;
using Stoolball.Navigation;

namespace Stoolball.Statistics
{
    public interface IStatisticsBreadcrumbBuilder
    {
        void BuildBreadcrumbs(List<Breadcrumb> breadcrumbs, StatisticsFilter filter);
    }
}