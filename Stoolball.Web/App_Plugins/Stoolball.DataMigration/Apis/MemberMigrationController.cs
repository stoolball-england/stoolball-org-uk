using Newtonsoft.Json;
using Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators;
using Stoolball.Web.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Http;
using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Editors;
using Umbraco.Web.Models.ContentEditing;
using Umbraco.Web.Mvc;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.Apis
{
    [PluginController("Migration")]
    public class MemberMigrationController : UmbracoAuthorizedJsonController
    {
        private readonly IApiKeyProvider _apiKeyProvider;

        public MemberMigrationController(IGlobalSettings globalSettings,
            IUmbracoContextAccessor umbracoContextAccessor,
            ISqlContext sqlContext,
            ServiceContext serviceContext,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IRuntimeState runtimeState,
            UmbracoHelper umbracoHelper,
            IApiKeyProvider apiKeyProvider) :
            base(globalSettings, umbracoContextAccessor, sqlContext, serviceContext, appCaches, profilingLogger, runtimeState, umbracoHelper)
        {
            _apiKeyProvider = apiKeyProvider ?? throw new ArgumentNullException(nameof(apiKeyProvider));
        }

        [HttpGet]
        public string ApiKey() => _apiKeyProvider.GetApiKey("DataMigration");

        [HttpPost]
        public IHttpActionResult CreateMember(MigratedMember imported)
        {
            if (imported is null)
            {
                throw new ArgumentNullException(nameof(imported));
            }

            // If there's an existing member update it; don't create a second member with the same email address.
            var member = Services.MemberService.GetByEmail(imported.Email);
            var newMember = (member == null);
            if (newMember)
            {
                var memberType = Services.MemberTypeService.Get("Member");
                member = Services.MemberService.CreateMemberWithIdentity(imported.Email, imported.Email, imported.Name, memberType);
            }
            else
            {
                member.Name = imported.Name;
            }
            member.SetValue("migratedMemberId", imported.UserId);
            member.SetValue("totalLogins", imported.TotalLogins);
            member.LastLoginDate = imported.LastLogin;
            member.CreateDate = imported.DateCreated;
            member.UpdateDate = imported.DateUpdated;
            member.IsApproved = true;
            member.IsLockedOut = false;
            member.SetValue("blockLogin", false);
            Services.MemberService.Save(member);

            // Give them a complex password that nobody will guess. They will need to use password reset.
            Services.MemberService.SavePassword(member, Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));

            if (newMember)
            {
                Services.MemberService.AssignRole(member.Id, Groups.AllMembers);
            }
            return Created(new Uri(Request.RequestUri, new Uri($"/umbraco#/member/member/edit/{member.Key}", UriKind.Relative)), JsonConvert.SerializeObject(member));
        }

        [HttpGet]
        public IEnumerable<IMemberGroup> MemberGroups() => Services.MemberGroupService.GetAll();

        [HttpPost]
        public IHttpActionResult DeleteMemberGroup(MemberGroup group)
        {
            Services.MemberGroupService.Delete(group);
            return Ok();
        }

        [HttpPost]
        public IHttpActionResult CreateMemberGroup(MemberGroupSave saveModel)
        {
            if (saveModel is null)
            {
                throw new ArgumentNullException(nameof(saveModel));
            }

            // Create the group unless it already exists
            var group = Services.MemberGroupService.GetByName(saveModel.Name);

            if (group == null)
            {
                group = new MemberGroup
                {
                    Name = saveModel.Name
                };
                Services.MemberGroupService.Save(group);
            }
            return Created(new Uri(Request.RequestUri, new Uri($"/umbraco#/member/memberGroups/edit/{group.Id}", UriKind.Relative)), JsonConvert.SerializeObject(group));
        }

        [HttpPost]
        public IHttpActionResult AssignMemberGroup(MemberGroupAssignment assignment)
        {
            if (assignment is null)
            {
                throw new ArgumentNullException(nameof(assignment));
            }

            var member = Members.GetByEmail(assignment.Email);
            if (member != null)
            {
                Services.MemberService.AssignRole(member.Id, assignment.GroupName);
            }

            return Ok();
        }
    }
}
