using System;
using NPoco;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
{
    [TableName(Tables.Audit)]
    [PrimaryKey(nameof(AuditId), AutoIncrement = false)]
    [ExplicitColumns]
    public class AuditTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(AuditId))]
        public Guid AuditId { get; set; }

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

        [Column(nameof(RedactedState))]
        [SpecialDbType(SpecialDbTypes.NTEXT)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string RedactedState { get; set; }

        [Column(nameof(AuditDate))]
        [Index(IndexTypes.NonClustered)]
        public DateTime AuditDate { get; set; }
    }
}