using Umbraco.Cms.Core.Composing;

namespace Stoolball.Data.UmbracoMigrations
{
    /// <summary>
    /// An Umbraco composer which runs on startup and loads <see cref="StoolballDataComponent"/>, which kicks off the <see cref="StoolballDataMigrationPlan"/>
    /// </summary>
    public class StoolballDataComposer : ComponentComposer<StoolballDataComponent>, IComposer
    {
    }
}