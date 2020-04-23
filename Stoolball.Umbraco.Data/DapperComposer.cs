using Dapper;
using Umbraco.Core.Composing;

namespace Stoolball.Umbraco.Data
{
    public class DapperComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            // Tell Dapper that dates going into and out of the database are in UTC
            SqlMapper.AddTypeHandler(new DapperDateTimeHandler());
        }
    }
}