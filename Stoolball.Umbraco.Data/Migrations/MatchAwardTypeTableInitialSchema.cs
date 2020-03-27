using NPoco;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data.Migrations
{
    [TableName(Constants.Tables.MatchAwardType)]
    [PrimaryKey(nameof(MatchAwardTypeId), AutoIncrement = true)]
    [ExplicitColumns]
    public class MatchAwardTypeTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
        [Column(nameof(MatchAwardTypeId))]
        public int MatchAwardTypeId { get; set; }

        [Column(nameof(MatchAwardTypeName))]
        public string MatchAwardTypeName { get; set; }
    }
}