using Dapper;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Stoolball.Web
{
    public class DapperComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            // Tell Dapper that dates going into and out of the database are in UTC
            SqlMapper.AddTypeHandler(new DapperDateTimeHandler());
        }
    }
}