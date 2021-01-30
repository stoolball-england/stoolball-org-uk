using System;
using NPoco;
using Stoolball.Data.SqlServer;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
{
    [TableName(Tables.SeasonTeam)]
    [PrimaryKey(nameof(SeasonTeamId), AutoIncrement = false)]
    [ExplicitColumns]
    public class SeasonTeamTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(SeasonTeamId))]
        public Guid SeasonTeamId { get; set; }

        [ForeignKey(typeof(SeasonTableInitialSchema), Column = nameof(SeasonTableInitialSchema.SeasonId))]
        [Index(IndexTypes.Clustered)]
        [Column(nameof(SeasonId))]
        public Guid SeasonId { get; set; }

        [ForeignKey(typeof(TeamTableInitialSchema), Column = nameof(TeamTableInitialSchema.TeamId))]
        [Index(IndexTypes.NonClustered)]
        [Column(nameof(TeamId))]
        public Guid TeamId { get; set; }

        [Column(nameof(WithdrawnDate))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public DateTime? WithdrawnDate { get; set; }
    }
}