using System;
using System.Data;
using Dapper;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// Tell Dapper that dates going into and out of the database are in UTC
    /// </summary>
    public class DapperDateTimeHandler : SqlMapper.TypeHandler<DateTimeOffset>
    {
        public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
        {
            if (parameter != null)
            {
                parameter.Value = value;
            }
        }

        public override DateTimeOffset Parse(object value)
        {
            if (value != null)
            {
                return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(new DateTimeOffset((DateTime)value, TimeSpan.Zero), "GMT Standard Time");
            }

            return new DateTimeOffset();
        }
    }
}
