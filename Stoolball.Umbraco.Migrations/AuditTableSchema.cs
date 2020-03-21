using NPoco;
using System;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data
{
    [TableName(Constants.Tables.Audit)]
    [PrimaryKey(nameof(AuditId), AutoIncrement = true)]
    [ExplicitColumns]
    public class AuditTableSchema
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
        [Column(nameof(AuditId))]
        public int AuditId { get; set; }

        [Column(nameof(MemberKey))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public Guid? MemberKey { get; set; }

        [Column(nameof(RequestUrl))]
        [NullSetting(NullSetting = NullSettings.Null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "Uri does not match a SQL datatype")]
        public string RequestUrl { get; set; }

        [Column(nameof(ItemUri))]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "Uri does not match a SQL datatype")]
        public string ItemUri { get; set; }

        [Column(nameof(DataBefore))]
        [NullSetting(NullSetting = NullSettings.Null)]
        [SpecialDbType(SpecialDbTypes.NTEXT)]
        public string DataBefore { get; set; }

        [Column(nameof(DataAfter))]
        [NullSetting(NullSetting = NullSettings.Null)]
        [SpecialDbType(SpecialDbTypes.NTEXT)]
        public string DataAfter { get; set; }

        [Column(nameof(AuditDate))]
        public DateTime AuditDate { get; set; }
    }
}