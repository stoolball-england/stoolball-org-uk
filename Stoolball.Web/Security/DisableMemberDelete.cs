using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Events;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;

namespace Stoolball.Web.Security
{
    [RuntimeLevel(MinLevel = RuntimeLevel.Run)]
    public class DisableMemberDeleteEventComposer : ComponentComposer<DisableMemberDeleteEventComponent>
    { }

    /// <summary>
    /// See https://github.com/stoolball-england/stoolball-org-uk/issues/378
    /// </summary>
    public class DisableMemberDeleteEventComponent : IComponent
    {
        private readonly ILogger _logger;

        public DisableMemberDeleteEventComponent(ILogger logger)
        {
            _logger = logger;
        }

        public void Initialize()
        {
            MemberService.Deleting += MemberService_Deleting;
        }

        private void MemberService_Deleting(IMemberService sender, DeleteEventArgs<IMember> e)
        {
            foreach (var member in e.DeletedEntities)
            {
                e.CancelOperation(new EventMessage("Blocked", $"Deleting the member {member} has been cancelled. Deleting all members is disabled until the side-effects are handled.", EventMessageType.Warning));
                _logger.Info<DisableMemberDeleteEventComponent>("Deleting the member {member} has been cancelled. Deleting all members is disabled until the side-effects are handled.", member.Name);
            }
        }

        public void Terminate()
        {
            MemberService.Deleting -= MemberService_Deleting;
        }
    }
}
