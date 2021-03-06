﻿using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using Stoolball.Data.SqlServer;

namespace Stoolball.Web
{
    public class UmbracoDatabaseConnectionFactory : IDatabaseConnectionFactory
    {
        public IDbConnection CreateDatabaseConnection()
        {
            return new SqlConnection(ConfigurationManager.ConnectionStrings["UmbracoDbDsn"]?.ConnectionString);
        }
    }
}
