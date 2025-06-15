using System;
using Microsoft.Extensions.Logging;
using Stoolball.Security;
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
        private readonly IVerificationToken _verificationToken;

        public DisableMemberDeleteNotificationHandler(ILogger<DisableMemberDeleteNotificationHandler> logger, IVerificationToken verificationToken)
        {
            _logger = logger;
            _verificationToken = verificationToken;
        }

        public void Handle(MemberDeletingNotification notification)
        {
            foreach (var member in notification.DeletedEntities)
            {
                if (!_verificationToken.HasExpired(member.GetValue<DateTime>("approvalTokenExpires")) || member.GetValue<int>("totalLogins") > 0)
                {
                    notification.CancelOperation(new EventMessage("Blocked", $"Deleting the member {member} has been cancelled. Deleting all members is disabled until the side-effects are handled.", EventMessageType.Warning));
                    _logger.LogInformation("Deleting the member {member} has been cancelled. Deleting all members is disabled until the side-effects are handled.", member.Name);
                }
            }
        }
    }
}
