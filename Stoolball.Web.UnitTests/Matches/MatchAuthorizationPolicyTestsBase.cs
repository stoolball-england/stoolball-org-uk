using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Stoolball.Security;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Xunit;
using static Stoolball.Constants;

namespace Stoolball.Web.UnitTests.Matches
{
    /// <summary>
    /// Base class for testing match-like authorization policies that follow the IAuthorizationPolicy pattern
    /// where authorization can be based on individual member ownership, member groups, or both.
    /// </summary>
    /// <typeparam name="T">The entity type being authorized (e.g., Match, Tournament)</typeparam>
    public abstract class MatchAuthorizationPolicyTestsBase<T> where T : class
    {
        protected readonly Mock<IMemberManager> MemberManager = new();
        protected readonly Mock<IMemberService> MemberService = new();

        /// <summary>
        /// Creates an instance of the policy being tested. Must be implemented by derived classes.
        /// </summary>
        protected abstract IAuthorizationPolicy<T> CreatePolicy();

        /// <summary>
        /// Creates an entity with the specified member key. Must be implemented by derived classes.
        /// </summary>
        protected abstract T CreateEntity(Guid? memberKey);

        /// <summary>
        /// Gets the parameter name used when the entity is null (e.g., "tournament").
        /// </summary>
        protected abstract string EntityParameterName { get; }

        /// <summary>
        /// Gets the edit action type for the entity.
        /// </summary>
        protected abstract AuthorizedAction EditAction { get; }

        /// <summary>
        /// Gets the delete action type for the entity.
        /// </summary>
        protected abstract AuthorizedAction DeleteAction { get; }

        [Fact]
        public async Task IsAuthorized_throws_ArgumentNullException_when_entity_is_null()
        {
            var policy = CreatePolicy();

            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                policy.IsAuthorized(null!));
            Assert.Equal(EntityParameterName, ex.ParamName);
        }

        [Theory]
        [InlineData(nameof(EditAction))]
        [InlineData(nameof(DeleteAction))]
        public async Task IsAuthorized_action_returns_false_when_no_current_member(string actionPropertyName)
        {
            var policy = CreatePolicy();
            var entity = CreateEntity(Guid.NewGuid());
            var action = GetActionByPropertyName(actionPropertyName);

            MemberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult<MemberIdentityUser?>(null));

            var result = await policy.IsAuthorized(entity);

            Assert.False(result[action]);
        }

        [Theory]
        [InlineData(nameof(EditAction))]
        [InlineData(nameof(DeleteAction))]
        public async Task IsAuthorized_action_returns_true_when_member_is_creator(string actionPropertyName)
        {
            var policy = CreatePolicy();
            var memberKey = Guid.NewGuid();
            var entity = CreateEntity(memberKey);
            var currentMember = new MemberIdentityUser { Key = memberKey };
            var action = GetActionByPropertyName(actionPropertyName);

            MemberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult<MemberIdentityUser?>(currentMember));

            var result = await policy.IsAuthorized(entity);

            Assert.True(result[action]);
        }

        [Theory]
        [InlineData(nameof(EditAction))]
        [InlineData(nameof(DeleteAction))]
        public async Task IsAuthorized_action_returns_true_when_member_is_administrator(string actionPropertyName)
        {
            var policy = CreatePolicy();
            var creatorKey = Guid.NewGuid();
            var memberKey = Guid.NewGuid();
            var entity = CreateEntity(creatorKey);
            var currentMember = new MemberIdentityUser { Key = memberKey };
            var action = GetActionByPropertyName(actionPropertyName);

            MemberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult<MemberIdentityUser?>(currentMember));
            MemberManager.Setup(x => x.IsMemberAuthorizedAsync(null, new[] { Groups.Administrators }, null))
                .Returns(Task.FromResult(true));

            var result = await policy.IsAuthorized(entity);

            Assert.True(result[action]);
        }

        [Theory]
        [InlineData(nameof(EditAction))]
        [InlineData(nameof(DeleteAction))]
        public async Task IsAuthorized_action_returns_false_when_not_creator_and_not_administrator(string actionPropertyName)
        {
            var policy = CreatePolicy();
            var creatorKey = Guid.NewGuid();
            var memberKey = Guid.NewGuid();
            var entity = CreateEntity(creatorKey);
            var currentMember = new MemberIdentityUser { Key = memberKey };
            var action = GetActionByPropertyName(actionPropertyName);

            MemberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult<MemberIdentityUser?>(currentMember));
            MemberManager.Setup(x => x.IsMemberAuthorizedAsync(null, new[] { Groups.Administrators }, null))
                .Returns(Task.FromResult(false));

            var result = await policy.IsAuthorized(entity);

            Assert.False(result[action]);
        }

        [Fact]
        public void AuthorizedGroupNames_returns_empty_list()
        {
            var policy = CreatePolicy();
            var entity = CreateEntity(null);

            var result = policy.AuthorizedGroupNames(entity);

            Assert.Empty(result);
        }

        [Fact]
        public async Task AuthorizedMemberNames_throws_ArgumentNullException_when_entity_is_null()
        {
            var policy = CreatePolicy();

            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                policy.AuthorizedMemberNames(null!));
            Assert.Equal(EntityParameterName, ex.ParamName);
        }

        [Fact]
        public async Task AuthorizedMemberNames_returns_entity_creator()
        {
            var policy = CreatePolicy();
            var creatorKey = Guid.NewGuid();
            var entity = CreateEntity(creatorKey);
            var currentMember = new MemberIdentityUser { Key = Guid.NewGuid() };

            var creator = CreateMember("creator", "Entity Creator");
            MemberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult<MemberIdentityUser?>(currentMember));
            MemberService.Setup(x => x.GetByKey(creatorKey)).Returns(creator);

            var result = await policy.AuthorizedMemberNames(entity);

            Assert.Single(result);
            Assert.Contains("Entity Creator", result);
        }

        [Fact]
        public async Task AuthorizedMemberNames_excludes_current_member_as_creator()
        {
            var policy = CreatePolicy();
            var memberKey = Guid.NewGuid();
            var entity = CreateEntity(memberKey);
            var currentMember = new MemberIdentityUser { Key = memberKey };

            MemberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult<MemberIdentityUser?>(currentMember));

            var result = await policy.AuthorizedMemberNames(entity);

            Assert.Empty(result);
        }

        [Fact]
        public async Task AuthorizedMemberNames_returns_empty_when_no_memberkey()
        {
            var policy = CreatePolicy();
            var entity = CreateEntity(null);

            MemberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult<MemberIdentityUser?>(null));

            var result = await policy.AuthorizedMemberNames(entity);

            Assert.Empty(result);
        }

        [Fact]
        public async Task AuthorizedMemberNames_handles_null_current_member()
        {
            var policy = CreatePolicy();
            var creatorKey = Guid.NewGuid();
            var entity = CreateEntity(creatorKey);

            var creator = CreateMember("creator", "Entity Creator");
            MemberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult<MemberIdentityUser?>(null));
            MemberService.Setup(x => x.GetByKey(creatorKey)).Returns(creator);

            var result = await policy.AuthorizedMemberNames(entity);

            Assert.Single(result);
            Assert.Contains("Entity Creator", result);
        }

        [Fact]
        public async Task AuthorizedMemberNames_excludes_null_member_name()
        {
            var policy = CreatePolicy();
            var creatorKey = Guid.NewGuid();
            var entity = CreateEntity(creatorKey);

            MemberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult<MemberIdentityUser?>(null));
            MemberService.Setup(x => x.GetByKey(creatorKey)).Returns((IMember?)null);

            var result = await policy.AuthorizedMemberNames(entity);

            Assert.Empty(result);
        }

        protected static IMember CreateMember(string username, string name)
        {
            var member = new Mock<IMember>();
            member.Setup(x => x.Username).Returns(username);
            member.Setup(x => x.Name).Returns(name);
            return member.Object;
        }

        private AuthorizedAction GetActionByPropertyName(string propertyName)
        {
            return propertyName == nameof(EditAction) ? EditAction : DeleteAction;
        }
    }
}
