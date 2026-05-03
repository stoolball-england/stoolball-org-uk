using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Stoolball.Security;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Xunit;
using static Stoolball.Constants;

namespace Stoolball.Web.UnitTests
{
    /// <summary>
    /// Base class for testing authorization policy implementations that follow the IAuthorizationPolicy pattern.
    /// </summary>
    /// <typeparam name="T">The entity type being authorized (e.g., Club, Competition)</typeparam>
    public abstract class AuthorizationPolicyTestsBase<T> where T : class
    {
        protected readonly Mock<IMemberManager> MemberManager = new();
        protected readonly Mock<IMemberService> MemberService = new();

        /// <summary>
        /// Creates an instance of the policy being tested. Must be implemented by derived classes.
        /// </summary>
        protected abstract IAuthorizationPolicy<T> CreatePolicy();

        /// <summary>
        /// Creates an entity with the specified member group name. Must be implemented by derived classes.
        /// </summary>
        protected abstract T CreateEntity(string? memberGroupName);

        /// <summary>
        /// Gets the parameter name used when the entity is null (e.g., "club" or "competition").
        /// </summary>
        protected abstract string EntityParameterName { get; }

        /// <summary>
        /// Gets the create action type for the entity.
        /// </summary>
        protected abstract AuthorizedAction CreateAction { get; }

        /// <summary>
        /// Gets the edit action type for the entity.
        /// </summary>
        protected abstract AuthorizedAction EditAction { get; }

        /// <summary>
        /// Gets the delete action type for the entity.
        /// </summary>
        protected abstract AuthorizedAction DeleteAction { get; }

        protected void SetupMemberManager(IEnumerable<string> groupsForUser)
        {
            Func<IEnumerable<string>, IEnumerable<string>, bool> userIsInGroup = (groupsForUser, allowedGroups) => groupsForUser.Any(groupForUser => allowedGroups.Contains(groupForUser));

            MemberManager.Setup(x => x.IsMemberAuthorizedAsync(
                    null,
                    It.Is<IEnumerable<string>>(allowedGroups => userIsInGroup(groupsForUser, allowedGroups)),
                    null))
                .Returns(Task.FromResult(true));

            MemberManager.Setup(x => x.IsMemberAuthorizedAsync(
                    null,
                    It.Is<IEnumerable<string>>(allowedGroups => !userIsInGroup(groupsForUser, allowedGroups)),
                    null))
                .Returns(Task.FromResult(false));
        }

        [Fact]
        public void AuthorizedGroupNames_throws_ArgumentNullException_when_entity_is_null()
        {
            var policy = CreatePolicy();

            var ex = Assert.Throws<ArgumentNullException>(() =>
                policy.AuthorizedGroupNames(null!));
            Assert.Equal(EntityParameterName, ex.ParamName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void AuthorizedGroupNames_returns_empty_list_when_MemberGroupName_is_null_or_empty(string? memberGroupName)
        {
            var policy = CreatePolicy();
            var entity = CreateEntity(memberGroupName);

            var result = policy.AuthorizedGroupNames(entity);

            Assert.Empty(result);
        }

        [Fact]
        public void AuthorizedGroupNames_returns_member_group_name()
        {
            var policy = CreatePolicy();
            var entity = CreateEntity("EntityManagers");

            var result = policy.AuthorizedGroupNames(entity);

            Assert.Single(result);
            Assert.Equal("EntityManagers", result[0]);
        }

        [Fact]
        public async Task AuthorizedMemberNames_throws_ArgumentNullException_when_entity_is_null()
        {
            var policy = CreatePolicy();

            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                policy.AuthorizedMemberNames(null!));
            Assert.Equal(EntityParameterName, ex.ParamName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task AuthorizedMemberNames_returns_empty_list_when_MemberGroupName_is_null_or_empty(string? memberGroupName)
        {
            var policy = CreatePolicy();
            var entity = CreateEntity(memberGroupName);

            var result = await policy.AuthorizedMemberNames(entity);

            Assert.Empty(result);
        }

        [Fact]
        public async Task AuthorizedMemberNames_returns_members_in_group_excluding_current_member()
        {
            var policy = CreatePolicy();
            var entity = CreateEntity("TestGroup");
            var currentMember = new MemberIdentityUser { UserName = "currentuser" };

            var groupMembers = new[]
            {
                CreateMember("member1", "Member One"),
                CreateMember("currentuser", "Current User"),
                CreateMember("member2", "Member Two")
            };

            MemberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult<MemberIdentityUser?>(currentMember));
            MemberService.Setup(x => x.GetMembersInRole("TestGroup")).Returns(groupMembers);

            var result = await policy.AuthorizedMemberNames(entity);

            Assert.Equal(2, result.Count);
            Assert.Contains("Member One", result);
            Assert.Contains("Member Two", result);
            Assert.DoesNotContain("Current User", result);
        }

        [Fact]
        public async Task AuthorizedMemberNames_handles_null_current_member()
        {
            var policy = CreatePolicy();
            var entity = CreateEntity("TestGroup");

            var groupMembers = new[]
            {
                CreateMember("member1", "Member One"),
                CreateMember("member2", "Member Two")
            };

            MemberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult<MemberIdentityUser?>(null));
            MemberService.Setup(x => x.GetMembersInRole("TestGroup")).Returns(groupMembers);

            var result = await policy.AuthorizedMemberNames(entity);

            Assert.Equal(2, result.Count);
            Assert.Contains("Member One", result);
            Assert.Contains("Member Two", result);
        }

        [Fact]
        public async Task IsAuthorized_throws_ArgumentNullException_when_entity_is_null()
        {
            var policy = CreatePolicy();

            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                policy.IsAuthorized(null!));
            Assert.Equal(EntityParameterName, ex.ParamName);
        }

        [Theory]
        [InlineData(new[] { Groups.AllMembers }, true)]
        [InlineData(new[] { Groups.AllMembers, Groups.Administrators }, true)]
        [InlineData(new[] { Groups.AllMembers, Groups.PowerUsers }, true)]
        [InlineData(new[] { Groups.AllMembers, Groups.PowerUsers, Groups.Administrators }, true)]
        [InlineData(new[] { Groups.AllMembers, "EntityOwners" }, true)]
        [InlineData(new[] { Groups.AllMembers, "EntityOwners", Groups.Administrators }, true)]
        [InlineData(new[] { Groups.AllMembers, "EntityOwners", Groups.PowerUsers }, true)]
        [InlineData(new[] { Groups.AllMembers, "EntityOwners", Groups.PowerUsers, Groups.Administrators }, true)]
        [InlineData(new[] { "SomeOtherGroup" }, false)]
        [InlineData(new string[0], false)]
        public async Task IsAuthorized_CreateAction_checks_group_membership(IEnumerable<string> groupsForUser, bool expectAuthorized)
        {
            var policy = CreatePolicy();
            var entity = CreateEntity(null);

            SetupMemberManager(groupsForUser);

            var result = await policy.IsAuthorized(entity);

            Assert.Equal(expectAuthorized, result[CreateAction]);
        }

        [Theory]
        [InlineData(new[] { Groups.AllMembers }, true, false)]
        [InlineData(new[] { Groups.AllMembers }, false, false)]
        [InlineData(new[] { Groups.AllMembers, Groups.PowerUsers }, true, false)]
        [InlineData(new[] { Groups.AllMembers, Groups.PowerUsers }, false, false)]
        [InlineData(new[] { Groups.AllMembers, Groups.Administrators }, true, true)]
        [InlineData(new[] { Groups.AllMembers, Groups.Administrators }, false, true)]
        [InlineData(new[] { Groups.AllMembers, "EntityOwners" }, true, false)]
        [InlineData(new[] { Groups.AllMembers, "EntityOwners" }, false, false)]
        [InlineData(new[] { Groups.AllMembers, "EntityOwners", Groups.PowerUsers }, true, false)]
        [InlineData(new[] { Groups.AllMembers, "EntityOwners", Groups.PowerUsers }, false, false)]
        [InlineData(new[] { Groups.AllMembers, "EntityOwners", Groups.Administrators }, true, true)]
        [InlineData(new[] { Groups.AllMembers, "EntityOwners", Groups.Administrators }, false, true)]
        [InlineData(new[] { "SomeOtherGroup" }, true, false)]
        [InlineData(new[] { "SomeOtherGroup" }, false, false)]
        [InlineData(new string[0], true, false)]
        [InlineData(new string[0], false, false)]
        public async Task IsAuthorized_DeleteAction_checks_group_membership(IEnumerable<string> groupsForUser, bool entityHasMemberGroupName, bool expectAuthorized)
        {
            var policy = CreatePolicy();
            var entity = CreateEntity(entityHasMemberGroupName ? "EntityOwners" : null);

            SetupMemberManager(groupsForUser);

            var result = await policy.IsAuthorized(entity);

            Assert.Equal(expectAuthorized, result[DeleteAction]);
        }

        [Theory]
        [InlineData(new[] { Groups.AllMembers }, true, false)]
        [InlineData(new[] { Groups.AllMembers }, false, false)]
        [InlineData(new[] { Groups.AllMembers, Groups.PowerUsers }, true, true)]
        [InlineData(new[] { Groups.AllMembers, Groups.PowerUsers }, false, true)]
        [InlineData(new[] { Groups.AllMembers, Groups.Administrators }, true, true)]
        [InlineData(new[] { Groups.AllMembers, Groups.Administrators }, false, true)]
        [InlineData(new[] { Groups.AllMembers, Groups.PowerUsers, Groups.Administrators }, true, true)]
        [InlineData(new[] { Groups.AllMembers, Groups.PowerUsers, Groups.Administrators }, false, true)]
        [InlineData(new[] { Groups.AllMembers, "EntityOwners" }, true, true)]
        [InlineData(new[] { Groups.AllMembers, "EntityOwners" }, false, false)]
        [InlineData(new[] { Groups.AllMembers, "EntityOwners", Groups.PowerUsers }, true, true)]
        [InlineData(new[] { Groups.AllMembers, "EntityOwners", Groups.PowerUsers }, false, true)]
        [InlineData(new[] { Groups.AllMembers, "EntityOwners", Groups.Administrators }, true, true)]
        [InlineData(new[] { Groups.AllMembers, "EntityOwners", Groups.Administrators }, false, true)]
        [InlineData(new[] { Groups.AllMembers, "EntityOwners", Groups.PowerUsers, Groups.Administrators }, true, true)]
        [InlineData(new[] { Groups.AllMembers, "EntityOwners", Groups.PowerUsers, Groups.Administrators }, false, true)]
        [InlineData(new[] { "SomeOtherGroup" }, true, false)]
        [InlineData(new[] { "SomeOtherGroup" }, false, false)]
        [InlineData(new string[0], true, false)]
        [InlineData(new string[0], false, false)]
        public async Task IsAuthorized_EditAction_checks_group_membership(IEnumerable<string> groupsForUser, bool entityHasMemberGroupName, bool expectAuthorized)
        {
            var policy = CreatePolicy();
            var entity = CreateEntity(entityHasMemberGroupName ? "EntityOwners" : null);

            SetupMemberManager(groupsForUser);

            var result = await policy.IsAuthorized(entity);

            Assert.Equal(expectAuthorized, result[EditAction]);
        }

        protected static IMember CreateMember(string username, string name)
        {
            var member = new Mock<IMember>();
            member.Setup(x => x.Username).Returns(username);
            member.Setup(x => x.Name).Returns(name);
            return member.Object;
        }
    }
}
