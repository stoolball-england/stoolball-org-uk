# nullable disable // Point-in-time snapshot should not be changed
using System;
using NPoco;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
{
    [TableName(Tables.Player)]
    [PrimaryKey(nameof(PlayerId), AutoIncrement = false)]
    [ExplicitColumns]
    public class PlayerTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(PlayerId))]
        public Guid PlayerId { get; set; }

        [Column(nameof(PlayerRoute))]
        [Index(IndexTypes.Clustered)]
        public string PlayerRoute { get; set; }
    }
}