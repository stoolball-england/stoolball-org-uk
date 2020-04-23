using Dapper;
using System;
using System.Data;

namespace Stoolball.Umbraco.Data
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
                return DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc);
            }

            return new DateTimeOffset();
        }
    }
}
