using System;
using NPoco;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
{
    [TableName(Tables.SeasonMatchType)]
    [PrimaryKey(nameof(SeasonMatchTypeId), AutoIncrement = false)]
    [ExplicitColumns]
    public class SeasonMatchTypeTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(SeasonMatchTypeId))]
        public Guid SeasonMatchTypeId { get; set; }

        [ForeignKey(typeof(SeasonTableInitialSchema), Column = nameof(SeasonTableInitialSchema.SeasonId))]
        [Index(IndexTypes.Clustered)]
        [Column(nameof(SeasonId))]
        public Guid SeasonId { get; set; }

        [Column(nameof(MatchType))]
        public string MatchType { get; set; }

    }
}