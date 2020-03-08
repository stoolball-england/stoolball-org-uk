using Stoolball.Web.Email;
using Umbraco.Core;
using Umbraco.Core.Composing;

namespace Stoolball.Web
{
    public class DependencyInjectionComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            composition.Register<IEmailHelper, EmailHelper>(Lifetime.Singleton);
        }
    }
}