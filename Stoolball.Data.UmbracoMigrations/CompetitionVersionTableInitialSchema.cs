using System;
using NPoco;
using Stoolball.Data.SqlServer;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
{
    [TableName(Tables.CompetitionVersion)]
    [PrimaryKey(nameof(CompetitionVersionId), AutoIncrement = false)]
    [ExplicitColumns]
    public class CompetitionVersionTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(CompetitionVersionId))]
        public Guid CompetitionVersionId { get; set; }

        [Column(nameof(CompetitionId))]
        [ForeignKey(typeof(CompetitionTableInitialSchema), Column = nameof(CompetitionTableInitialSchema.CompetitionId))]
        [Index(IndexTypes.Clustered)]
        public Guid CompetitionId { get; set; }

        [Column(nameof(CompetitionName))]
        public string CompetitionName { get; set; }

        [Column(nameof(ComparableName))]
        public string ComparableName { get; set; }

        [Column(nameof(FromDate))]
        [Index(IndexTypes.NonClustered)]
        public DateTime FromDate { get; set; }

        [Column(nameof(UntilDate))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public DateTime? UntilDate { get; set; }
    }
}