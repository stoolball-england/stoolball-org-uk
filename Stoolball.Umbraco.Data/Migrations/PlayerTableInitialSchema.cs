using System;
using NPoco;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data.Migrations
{
    [TableName(Constants.Tables.Player)]
    [PrimaryKey(nameof(PlayerId), AutoIncrement = false)]
    [ExplicitColumns]
    public class PlayerTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(PlayerId))]
        public Guid PlayerId { get; set; }

        [Column(nameof(PlayerName))]
        public string PlayerName { get; set; }

        [Column(nameof(PlayerRoute))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string PlayerRoute { get; set; }
    }
}