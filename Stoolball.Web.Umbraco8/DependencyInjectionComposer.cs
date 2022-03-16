using System;
using Stoolball.Email;
using Umbraco.Core;
using Umbraco.Core.Composing;

namespace Stoolball.Web
{
    public class DependencyInjectionComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            if (composition is null)
            {
                throw new ArgumentNullException(nameof(composition));
            }

            // Utility classes
            composition.Register<IContactDetailsParser, ContactDetailsParser>();
        }
    }
}