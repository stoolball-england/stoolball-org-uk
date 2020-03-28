using NPoco;
using System;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Stoolball.Umbraco.Data.Migrations
{
    [TableName(Constants.Tables.Audit)]
    [PrimaryKey(nameof(AuditId), AutoIncrement = true)]
    [ExplicitColumns]
    public class AuditTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1, Clustered = false)]
        [Column(nameof(AuditId))]
        public int AuditId { get; set; }

        [Column(nameof(MemberKey))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public Guid? MemberKey { get; set; }

        [Column(nameof(ActorName))]
        public string ActorName { get; set; }

        [Column(nameof(Action))]
        public string Action { get; set; }

        [Column(nameof(EntityUri))]
        [Index(IndexTypes.Clustered)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "Uri does not match a SQL Server data type")]
        public string EntityUri { get; set; }

        [Column(nameof(State))]
        [SpecialDbType(SpecialDbTypes.NTEXT)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string State { get; set; }

        [Column(nameof(AuditDate))]
        [Index(IndexTypes.NonClustered)]
        public DateTime AuditDate { get; set; }
    }
}