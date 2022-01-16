using System;

namespace Stoolball.Web.Statistics
{
    public interface IBackgroundTaskTracker
    {
        void IncrementCompletedBy(Guid taskId, int completed);
        void IncrementErrorsBy(Guid taskId, int errors);
        int ProgressPercentage(Guid taskId);
        void SetTarget(Guid taskId, int target);
        int TotalErrors(Guid taskId);
    }
}