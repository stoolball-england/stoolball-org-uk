# nullable disable // Point-in-time snapshot should not be changed
using System;
using NPoco;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
{
    [TableName(Tables.PointsRule)]
    [PrimaryKey(nameof(PointsRuleId), AutoIncrement = false)]
    [ExplicitColumns]
    public class PointsRuleTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(PointsRuleId))]
        public Guid PointsRuleId { get; set; }

        [Column(nameof(MatchResultType))]
        public string MatchResultType { get; set; }

        [Column(nameof(HomePoints))]
        public int HomePoints { get; set; }

        [Column(nameof(AwayPoints))]
        public int AwayPoints { get; set; }

        [Column(nameof(SeasonId))]
        [ForeignKey(typeof(SeasonTableInitialSchema), Column = nameof(SeasonTableInitialSchema.SeasonId))]
        [Index(IndexTypes.Clustered)]
        public Guid SeasonId { get; set; }
    }
}