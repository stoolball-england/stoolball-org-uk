﻿using System;
using NPoco;
using Stoolball.Data.SqlServer;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
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
        public Guid MatchId { get; set; }

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