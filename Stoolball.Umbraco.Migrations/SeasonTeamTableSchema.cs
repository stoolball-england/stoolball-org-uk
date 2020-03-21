using NPoco;
using System;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data
{
    [TableName(Constants.Tables.SeasonTeam)]
    [PrimaryKey(nameof(SeasonTeamId), AutoIncrement = true)]
    [ExplicitColumns]
    public class SeasonTeamTableSchema
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1, Clustered = false)]
        [Column(nameof(SeasonTeamId))]
        public int SeasonTeamId { get; set; }

        [ForeignKey(typeof(SeasonTableSchema), Column = nameof(SeasonTableSchema.SeasonId))]
        [Index(IndexTypes.Clustered)]
        [Column(nameof(SeasonId))]
        public int SeasonId { get; set; }

        [ForeignKey(typeof(TeamTableSchema), Column = nameof(TeamTableSchema.TeamId))]
        [Index(IndexTypes.NonClustered)]
        [Column(nameof(TeamId))]
        public int TeamId { get; set; }

        [Column(nameof(WithdrawnDate))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public DateTime? WithdrawnDate { get; set; }
    }
}