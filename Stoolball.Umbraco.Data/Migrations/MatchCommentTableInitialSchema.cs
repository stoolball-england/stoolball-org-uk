using NPoco;
using System;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data.Migrations
{
    [TableName(Constants.Tables.MatchComment)]
    [PrimaryKey(nameof(MatchCommentId), AutoIncrement = false)]
    [ExplicitColumns]
    public class MatchCommentTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(MatchCommentId))]
        public Guid MatchCommentId { get; set; }

        [ForeignKey(typeof(MatchTableInitialSchema), Column = nameof(MatchTableInitialSchema.MatchId))]
        [Index(IndexTypes.Clustered)]
        [Column(nameof(MatchId))]
        public int MatchId { get; set; }

        [Column(nameof(MemberKey))]
        public Guid MemberKey { get; set; }

        [Column(nameof(CommentDate))]
        public DateTime CommentDate { get; set; }

        [Column(nameof(Comment))]
        [SpecialDbType(SpecialDbTypes.NTEXT)]
        public string Comment { get; set; }
    }
}