using System;
using System.Data.SqlTypes;
using System.Linq;
using Newtonsoft.Json;
using Stoolball.Logging;
using Umbraco.Core.Services;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class AuditHistoryBuilder : IAuditHistoryBuilder
    {
        private readonly ServiceContext _serviceContext;

        public AuditHistoryBuilder(ServiceContext serviceContext)
        {
            _serviceContext = serviceContext ?? throw new ArgumentNullException(nameof(serviceContext));
        }

        public void BuildInitialAuditHistory<T>(T original, T migrated, string actor, Func<T, T> redact) where T : IAuditable
        {
            if (redact is null)
            {
                throw new ArgumentNullException(nameof(redact));
            }

            var (creatingMemberGuid, creatingMemberName) = original.History.Count > 0 ? FindMember(original.History[0].ActorName) : (null, actor);
            migrated.History.Add(new AuditRecord
            {
                Action = AuditAction.Create,
                MemberKey = creatingMemberGuid,
                ActorName = !string.IsNullOrEmpty(creatingMemberName) ? creatingMemberName : actor,
                EntityUri = migrated.EntityUri,
                State = JsonConvert.SerializeObject(original),
                RedactedState = JsonConvert.SerializeObject(redact(original)),
                AuditDate = original.History.Count > 0 && original.History[0].AuditDate != DateTime.MinValue ? original.History[0].AuditDate : SqlDateTime.MinValue.Value
            });

            if (original.History.Count > 1 && original.History[1].AuditDate > original.History[0].AuditDate)
            {
                var (updatingMemberGuid, updatingMemberName) = FindMember(original.History[1].ActorName);
                migrated.History.Add(new AuditRecord
                {
                    Action = AuditAction.Update,
                    MemberKey = updatingMemberGuid,
                    ActorName = !string.IsNullOrEmpty(updatingMemberName) ? updatingMemberName : actor,
                    EntityUri = migrated.EntityUri,
                    State = JsonConvert.SerializeObject(original),
                    RedactedState = JsonConvert.SerializeObject(redact(original)),
                    AuditDate = original.History[1].AuditDate != DateTime.MinValue ? original.History[1].AuditDate : SqlDateTime.MinValue.Value
                });
            }

            migrated.History.Add(new AuditRecord
            {
                Action = AuditAction.Update,
                ActorName = actor,
                EntityUri = migrated.EntityUri,
                State = JsonConvert.SerializeObject(migrated),
                RedactedState = JsonConvert.SerializeObject(redact(migrated)),
                AuditDate = DateTimeOffset.UtcNow
            });
        }

        private (Guid? memberGuid, string memberName) FindMember(string actorName)
        {
            int memberId;
            if (!string.IsNullOrWhiteSpace(actorName) && int.TryParse(actorName, out memberId))
            {
                var member = _serviceContext.MemberService.GetMembersByPropertyValue("migratedMemberId", memberId).SingleOrDefault();
                if (member != null)
                {
                    return (member.Key, member.Name);
                }
            }

            return (null, null);
        }
    }
}