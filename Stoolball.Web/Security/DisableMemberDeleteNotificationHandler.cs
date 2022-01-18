using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Stoolball.Web.Security
{
    /// <summary>
    /// See https://github.com/stoolball-england/stoolball-org-uk/issues/378
    /// </summary>
    public class DisableMemberDeleteNotificationHandler : INotificationHandler<MemberDeletingNotification>
    {
        private readonly ILogger<DisableMemberDeleteNotificationHandler> _logger;

        public DisableMemberDeleteNotificationHandler(ILogger<DisableMemberDeleteNotificationHandler> logger)
        {
            _logger = logger;
        }

        public void Handle(MemberDeletingNotification notification)
        {
            foreach (var member in notification.DeletedEntities)
            {
                notification.CancelOperation(new EventMessage("Blocked", $"Deleting the member {member} has been cancelled. Deleting all members is disabled until the side-effects are handled.", EventMessageType.Warning));
                _logger.LogInformation("Deleting the member {member} has been cancelled. Deleting all members is disabled until the side-effects are handled.", member.Name);
            }
        }
    }
}
