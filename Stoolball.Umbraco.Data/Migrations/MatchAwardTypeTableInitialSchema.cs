using NPoco;
using System;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data.Migrations
{
    [TableName(Constants.Tables.MatchAwardType)]
    [PrimaryKey(nameof(MatchAwardTypeId), AutoIncrement = false)]
    [ExplicitColumns]
    public class MatchAwardTypeTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false)]
        [Column(nameof(MatchAwardTypeId))]
        public Guid MatchAwardTypeId { get; set; }

        [Column(nameof(MatchAwardTypeName))]
        public string MatchAwardTypeName { get; set; }
    }
}