using System;
using Stoolball.Email;
using Stoolball.Teams;
using Umbraco.Core.Composing;
using Umbraco.Core.Logging;
using Umbraco.Web.Scheduling;

namespace Stoolball.Web.Teams
{
    public class SpogoCsvExportComposer : ComponentComposer<SpogoCsvExportComponent>
    {
    }

    public class SpogoCsvExportComponent : IComponent, IDisposable
    {
        private readonly IProfilingLogger _logger;
        private readonly ITeamDataSource _teamDataSource;
        private readonly IContactDetailsParser _contactDetailsParser;
        private BackgroundTaskRunner<IBackgroundTask> _taskRunner;
        private IBackgroundTask _task;
        private bool _disposedValue;

        public SpogoCsvExportComponent(IProfilingLogger logger, ITeamDataSource teamDataSource, IContactDetailsParser contactDetailsParser)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _contactDetailsParser = contactDetailsParser ?? throw new ArgumentNullException(nameof(contactDetailsParser));
            _taskRunner = new BackgroundTaskRunner<IBackgroundTask>(nameof(SpogoCsvExportTask), _logger);
        }

        public void Initialize()
        {
            var delayBeforeWeStart = 0;
            var howOftenWeRepeat = 1000 * 60 * 60 * 24; // 24 hours

            _task = new SpogoCsvExportTask(_taskRunner, delayBeforeWeStart, howOftenWeRepeat, _logger, _teamDataSource, _contactDetailsParser);

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