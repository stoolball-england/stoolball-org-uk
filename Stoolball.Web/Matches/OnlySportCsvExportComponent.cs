using System;
using Stoolball.Matches;
using Umbraco.Core.Composing;
using Umbraco.Core.Logging;
using Umbraco.Web.Scheduling;

namespace Stoolball.Web.Matches
{
    public class OnlySportCsvExportComposer : ComponentComposer<OnlySportCsvExportComponent>
    {
    }

    public class OnlySportCsvExportComponent : IComponent, IDisposable
    {
        private readonly IProfilingLogger _logger;
        private readonly IMatchListingDataSource _matchDataSource;
        private BackgroundTaskRunner<IBackgroundTask> _taskRunner;
        private IBackgroundTask _task;
        private bool _disposedValue;

        public OnlySportCsvExportComponent(IProfilingLogger logger, IMatchListingDataSource matchDataSource)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _taskRunner = new BackgroundTaskRunner<IBackgroundTask>(nameof(OnlySportCsvExportTask), _logger);
        }

        public void Initialize()
        {
            var delayBeforeWeStart = 0; // 1min
            var howOftenWeRepeat = 10000; // 5mins

            _task = new OnlySportCsvExportTask(_taskRunner, delayBeforeWeStart, howOftenWeRepeat, _logger, _matchDataSource);

            //As soon as we add our task to the runner it will start to run (after its delay period)
            _taskRunner.TryAdd(_task);
        }

        public void Terminate()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _task.Dispose();
                    _taskRunner.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}