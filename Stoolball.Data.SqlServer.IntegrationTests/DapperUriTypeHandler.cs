using System;
using System.Data;
using Dapper;

namespace Stoolball.Data.SqlServer.IntegrationTests
{
    public class DapperUriTypeHandler : SqlMapper.TypeHandler<Uri>
    {
        public override void SetValue(IDbDataParameter parameter, Uri value)
        {
            if (parameter is null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            parameter.Value = value?.ToString();
        }

        public override Uri Parse(object value)
        {
            return new Uri((string)value);
        }
    }
}
