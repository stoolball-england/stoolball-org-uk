
using NPoco;
using System;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data
{
    [TableName(Constants.Tables.Audit)]
    [PrimaryKey("AuditId", AutoIncrement = true)]
    [ExplicitColumns]
    public class AuditTableSchema
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
        [Column("AuditId")]
        public int AuditId { get; set; }

        [Column("MemberKey")]
        public Guid MemberKey { get; set; }

        [Column("RequestUrl")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "Uri does not match a SQL datatype")]
        public string RequestUrl { get; set; }

        [Column("DataBefore")]
        [SpecialDbType(SpecialDbTypes.NTEXT)]
        public string Before { get; set; }

        [Column("DataAfter")]
        [SpecialDbType(SpecialDbTypes.NTEXT)]
        public string After { get; set; }

        [Column("AuditDate")]
        public DateTime AuditDate { get; set; }
    }
}