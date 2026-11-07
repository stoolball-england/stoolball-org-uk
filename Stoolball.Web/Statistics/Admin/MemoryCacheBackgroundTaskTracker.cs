using System;
using System.Runtime.Caching;

namespace Stoolball.Web.Statistics.Admin
{
    public class MemoryCacheBackgroundTaskTracker : IBackgroundTaskTracker
    {
        ObjectCache _cache = MemoryCache.Default;

        private class TaskProgress
        {
            public int Completed { get; set; }
            public int Errors { get; set; }
            public int? Target { get; set; }
        }

        private TaskProgress ReadFromCache(Guid taskId)
        {
            if (_cache.Contains(taskId.ToString()))
            {
                return (TaskProgress)_cache.Get(taskId.ToString());
            }
            else return new TaskProgress();
        }

        private void UpdateCache(Guid taskId, TaskProgress progress)
        {
            if (progress is null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            _cache.Set(taskId.ToString(), progress, new CacheItemPolicy { SlidingExpiration = TimeSpan.FromHours(1) });
        }

        public void SetTarget(Guid taskId, int target)
        {
            var progress = ReadFromCache(taskId);
            progress.Target = target;
            UpdateCache(taskId, progress);
        }

        public void IncrementCompletedBy(Guid taskId, int completed)
        {
            var progress = ReadFromCache(taskId);
            progress.Completed += completed;
            UpdateCache(taskId, progress);
        }

        public void IncrementErrorsBy(Guid taskId, int errors)
        {
            var progress = ReadFromCache(taskId);
            progress.Errors += errors;
            UpdateCache(taskId, progress);
        }

        public int ProgressPercentage(Guid taskId)
        {
            var progress = ReadFromCache(taskId);
            if (progress.Target is null) return 0;
            if (progress.Target == 0) return 100;
            return (int)Math.Floor(((double)(progress.Completed + progress.Errors) / (double)progress.Target) * 100);
        }

        public int? Target(Guid taskId)
        {
            return ReadFromCache(taskId).Target;
        }

        public int TotalErrors(Guid taskId)
        {
            var progress = ReadFromCache(taskId);
            return progress.Errors;
        }
    }
}
