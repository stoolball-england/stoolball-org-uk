using System.Globalization;
using Humanizer.Configuration;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Stoolball.Web
{
    public class HumanizerComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            Configurator.CollectionFormatters.Register(CultureInfo.CurrentCulture.Name, new HumanizerCollectionGrammar());
        }
    }
}