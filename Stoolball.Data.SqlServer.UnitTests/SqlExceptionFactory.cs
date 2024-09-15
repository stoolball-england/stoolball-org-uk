using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.Data.SqlClient;

namespace Stoolball.Data.SqlServer.UnitTests
{
    /// <remarks>
    /// Adapted from https://stackoverflow.com/a/6751227/3169017 and https://gist.github.com/benjanderson/07e13d9a2068b32c2911
    /// </remarks>
    internal static class SqlExceptionFactory
    {
        internal const string ERROR_ESTABLISHING_CONNECTION = "An error has occurred while establishing a connection to the server. When connecting to SQL Server, this failure may be caused by the fact that under the default settings SQL Server does not allow remote connections.";
        internal const string ERROR_TIMEOUT = "Timeout expired. The timeout period elapsed prior to completion of the operation or the server is not responding.";

        internal static SqlException Create(SqlExceptionType exceptionType)
        {
            SqlException exception = Instantiate<SqlException>();

            var error = Instantiate<SqlError>();

            switch (exceptionType)
            {
                case SqlExceptionType.Connection:
                    SetProperty(exception, "_message", ERROR_ESTABLISHING_CONNECTION);
                    SetProperty(error, "_number", 53);
                    break;
                case SqlExceptionType.Timeout:
                    SetProperty(exception, "_message", ERROR_TIMEOUT);
                    SetProperty(error, "_number", -2);
                    break;
            }

            var errors = new List<object>();
            errors.Add(error);

            var errorCollection = Instantiate<SqlErrorCollection>();
            SetProperty(errorCollection, "_errors", errors);
            SetProperty(exception, "_errors", errorCollection);

            return exception;
        }

        private static T Instantiate<T>() where T : class
        {
            return (T)FormatterServices.GetUninitializedObject(typeof(T));
        }

        private static void SetProperty<T>(T targetObject, string fieldName, object value)
        {
            var field = typeof(T).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(targetObject, value);
            }
            else
            {
                throw new InvalidOperationException("No field with name " + fieldName);
            }
        }
    }
}
