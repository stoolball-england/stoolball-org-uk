﻿namespace Stoolball.Logging
{
    /// <summary>
    /// Defines the logging service.
    /// </summary>
    /// <remarks>
    /// <para>Message templates in logging methods follow the Message Templates specification
    /// available at https://messagetemplates.org/ in order to support structured logging.</para>
    /// <para>Implementations must ensure that they support these templates. Note that the
    /// specification includes support for traditional C# numeric placeholders.</para>
    /// <para>For instance, "Processed {Input} in {Time}ms."</para>
    /// </remarks>
    public interface ILogger<T>
    {
        Microsoft.Extensions.Logging.ILogger<T> Logger { get; }

        /// <summary>
        /// Logs a info message.
        /// </summary>
        /// <param name="messageTemplate">A message template.</param>
        /// <param name="propertyValues">Property values.</param>
        void Info(string messageTemplate, params object[] propertyValues);

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="messageTemplate">A message template.</param>
        /// <param name="propertyValues">Property values.</param>
        void Warn(string messageTemplate, params object[] propertyValues);

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="messageTemplate">A message template.</param>
        /// <param name="propertyValues">Property values.</param>
        void Error(string messageTemplate, params object[] propertyValues);
    }
}
