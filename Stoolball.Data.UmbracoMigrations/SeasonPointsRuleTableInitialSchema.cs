using System;
using NPoco;
using Stoolball.Data.SqlServer;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
{
    [TableName(Constants.Tables.SeasonPointsRule)]
    [PrimaryKey(nameof(SeasonPointsRuleId), AutoIncrement = false)]
    [ExplicitColumns]
    public class SeasonPointsRuleTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(SeasonPointsRuleId))]
        public Guid SeasonPointsRuleId { get; set; }

        [Column(nameof(SeasonId))]
        [ForeignKey(typeof(SeasonTableInitialSchema), Column = nameof(SeasonTableInitialSchema.SeasonId))]
        [Index(IndexTypes.Clustered)]
        public Guid SeasonId { get; set; }

        [Column(nameof(MatchResultType))]
        public string MatchResultType { get; set; }

        [Column(nameof(HomePoints))]
        public int HomePoints { get; set; }

        [Column(nameof(AwayPoints))]
        public int AwayPoints { get; set; }
    }
}