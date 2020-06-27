using NPoco;
using System;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data.Migrations
{
    [TableName(Constants.Tables.TournamentComment)]
    [PrimaryKey(nameof(TournamentCommentId), AutoIncrement = false)]
    [ExplicitColumns]
    public class TournamentCommentTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(TournamentCommentId))]
        public Guid TournamentCommentId { get; set; }

        [ForeignKey(typeof(TournamentTableInitialSchema), Column = nameof(TournamentTableInitialSchema.TournamentId))]
        [Index(IndexTypes.Clustered)]
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