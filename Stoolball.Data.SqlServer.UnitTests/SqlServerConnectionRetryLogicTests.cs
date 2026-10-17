using System;
using Xunit;

namespace Stoolball.Data.SqlServer.UnitTests
{
    public class SqlServerConnectionRetryLogicTests
    {
        [Fact]
        public void Login_failed_is_retried()
        {
            var provider = SqlServerConnectionRetryLogic.CreateProvider();

            Assert.True(provider.RetryLogic.TransientPredicate(SqlExceptionFactory.Create(SqlExceptionType.LoginFailed)));
        }

        [Fact]
        public void Timeout_is_not_retried()
        {
            var provider = SqlServerConnectionRetryLogic.CreateProvider();

            Assert.False(provider.RetryLogic.TransientPredicate(SqlExceptionFactory.Create(SqlExceptionType.Timeout)));
        }

        [Fact]
        public void Non_SQL_exception_is_not_retried()
        {
            var provider = SqlServerConnectionRetryLogic.CreateProvider();

            Assert.False(provider.RetryLogic.TransientPredicate(new InvalidOperationException("Timeout expired. All pooled connections were in use.")));
        }
    }
}
