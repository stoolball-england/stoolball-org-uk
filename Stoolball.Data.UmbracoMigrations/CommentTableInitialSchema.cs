using System;
using NPoco;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
{
    [TableName(Constants.Tables.Comment)]
    [PrimaryKey(nameof(CommentId), AutoIncrement = false)]
    [ExplicitColumns]
    public class CommentTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(CommentId))]
        public Guid CommentId { get; set; }

        [ForeignKey(typeof(MatchTableInitialSchema), Column = nameof(MatchTableInitialSchema.MatchId))]
        [Index(IndexTypes.Clustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        [Column(nameof(MatchId))]
        public Guid MatchId { get; set; }

        [ForeignKey(typeof(TournamentTableInitialSchema), Column = nameof(TournamentTableInitialSchema.TournamentId))]
        [Index(IndexTypes.NonClustered)]
        [NullSetting(NullSetting = NullSettings.Null)]
        [Column(nameof(TournamentId))]
        public Guid TournamentId { get; set; }

        [Column(nameof(MemberKey))]
        public Guid MemberKey { get; set; }

        [Column(nameof(MemberName))]
        public string MemberName { get; set; }

        [Column(nameof(CommentDate))]
        public DateTime CommentDate { get; set; }

        [Column(nameof(Comment))]
        [SpecialDbType(SpecialDbTypes.NTEXT)]
        public string Comment { get; set; }
    }
}