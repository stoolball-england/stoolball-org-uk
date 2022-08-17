using System;
using NPoco;
using Stoolball.Data.SqlServer;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace Stoolball.Data.UmbracoMigrations
{
    [TableName(Tables.MatchLocation)]
    [PrimaryKey(nameof(MatchLocationId), AutoIncrement = false)]
    [ExplicitColumns]
    public class MatchLocationTableInitialSchema
    {
        [PrimaryKeyColumn(AutoIncrement = false, Clustered = false)]
        [Column(nameof(MatchLocationId))]
        public Guid MatchLocationId { get; set; }

        [Column(nameof(MigratedMatchLocationId))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? MigratedMatchLocationId { get; set; }

        [Column(nameof(ComparableName))]
        [Index(IndexTypes.Clustered)]
        public string ComparableName { get; set; }

        [Column(nameof(SecondaryAddressableObjectName))]
        [NullSetting(NullSetting = NullSettings.Null)]
        [Length(100)]
        public string SecondaryAddressableObjectName { get; set; }

        [Column(nameof(PrimaryAddressableObjectName))]
        [NullSetting(NullSetting = NullSettings.Null)]
        [Length(100)]
        public string PrimaryAddressableObjectName { get; set; }

        [Column(nameof(StreetDescription))]
        [NullSetting(NullSetting = NullSettings.Null)]
        [Length(100)]
        public string StreetDescription { get; set; }

        [Column(nameof(Locality))]
        [NullSetting(NullSetting = NullSettings.Null)]
        [Length(35)]
        public string Locality { get; set; }

        [Column(nameof(Town))]
        [NullSetting(NullSetting = NullSettings.Null)]
        [Length(30)]
        public string Town { get; set; }

        [Column(nameof(AdministrativeArea))]
        [NullSetting(NullSetting = NullSettings.Null)]
        [Length(30)]
        public string AdministrativeArea { get; set; }

        [Column(nameof(Postcode))]
        [NullSetting(NullSetting = NullSettings.Null)]
        [Length(8)]
        public string Postcode { get; set; }

        [Column(nameof(Latitude))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public double Latitude { get; set; }

        [Column(nameof(Longitude))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public double Longitude { get; set; }

        [Column(nameof(GeoPrecision))]
        [ColumnType(typeof(string))]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string GeoPrecision { get; set; }

        [Column(nameof(MatchLocationNotes))]
        [NullSetting(NullSetting = NullSettings.Null)]
        [SpecialDbType(SpecialDbTypes.NTEXT)]
        public string MatchLocationNotes { get; set; }

        [Column(nameof(MemberGroupKey))]
        public Guid MemberGroupKey { get; set; }

        [Column(nameof(MemberGroupName))]
        public string MemberGroupName { get; set; }

        [Column(nameof(MatchLocationRoute))]
        [Index(IndexTypes.UniqueNonClustered)]
        public string MatchLocationRoute { get; set; }
    }
}