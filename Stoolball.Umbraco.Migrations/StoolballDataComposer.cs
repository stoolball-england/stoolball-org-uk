using Umbraco.Core.Composing;

namespace Stoolball.Umbraco.Data
{
    /// <summary>
    /// An Umbraco composer which runs on startup and loads <see cref="StoolballDataComponent"/>, which kicks off the <see cref="StoolballMigrationPlan"/>
    /// </summary>
    public class StoolballDataComposer : ComponentComposer<StoolballDataComponent>
    {
    }
}