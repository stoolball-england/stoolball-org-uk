using System;
using Microsoft.Extensions.Logging;

namespace Stoolball.Web.Logging
{
    /// <summary>
    /// Wraps the standard .NET ILogger so that calls can be verified in unit tests. 
    /// The standard ILogger uses extension methods that unit tests can't verify.
    /// </summary>
    public class LogWrapper<T> : Stoolball.Logging.ILogger<T>
    {
        private readonly ILogger<T> _logger;

        public ILogger<T> Logger { get { return _logger; } }

        public LogWrapper(ILogger<T> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Info(string messageTemplate, params object[] propertyValues)
        {
            _logger.LogInformation(messageTemplate, propertyValues);

        }

        public void Warn(string messageTemplate, params object[] propertyValues)
        {
            _logger.LogWarning(messageTemplate, propertyValues);
        }

        public void Error(string messageTemplate, params object[] propertyValues)
        {
            _logger.LogError(messageTemplate, propertyValues);
        }
    }
}