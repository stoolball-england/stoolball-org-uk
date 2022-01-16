using Humanizer.Configuration;
using System.Globalization;
using Umbraco.Core.Composing;

namespace Stoolball.Web
{
    public class HumanizerComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            Configurator.CollectionFormatters.Register(CultureInfo.CurrentCulture.Name, new HumanizerCollectionGrammar());
        }
    }
}