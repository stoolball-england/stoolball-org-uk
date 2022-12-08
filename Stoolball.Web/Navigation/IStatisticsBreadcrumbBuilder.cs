using System.Collections.Generic;
using Stoolball.Statistics;

namespace Stoolball.Web.Navigation
{
    public interface IStatisticsBreadcrumbBuilder
    {
        void BuildBreadcrumbs(List<Breadcrumb> breadcrumbs, StatisticsFilter filter);
    }
}