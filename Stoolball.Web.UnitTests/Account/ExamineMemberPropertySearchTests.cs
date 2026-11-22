using System;
using System.Linq;
using Examine;
using Examine.Search;
using Moq;
using Stoolball.Web.Account;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Examine;
using Xunit;
using static Umbraco.Cms.Core.Constants;

namespace Stoolball.Web.UnitTests.Account
{
    public class ExamineMemberPropertySearchTests
    {
        private const string PROPERTY_ALIAS = "requestedEmail";
        private readonly Mock<IExamineManager> _examineManager = new();
        private readonly Mock<IMemberService> _memberService = new();
        private readonly Mock<IBooleanOperation> _query = new();

        public ExamineMemberPropertySearchTests()
        {
            var searcher = new Mock<ISearcher>();
            var query = new Mock<IQuery>();
            searcher.Setup(x => x.CreateQuery(IndexTypes.Member, It.IsAny<BooleanOperation>())).Returns(query.Object);
            query.Setup(x => x.Field(PROPERTY_ALIAS, It.IsAny<IExamineValue>())).Returns(_query.Object);

            var index = new Mock<IIndex>();
            index.Setup(x => x.Searcher).Returns(searcher.Object);
            var indexObject = index.Object;
            _examineManager.Setup(x => x.TryGetIndex(UmbracoIndexes.MembersIndexName, out indexObject)).Returns(true);
        }

        private void SetupSearchResults(params int[] memberIds)
        {
            var results = memberIds.Select(id => Mock.Of<ISearchResult>(x => x.Id == id.ToString(System.Globalization.CultureInfo.InvariantCulture))).ToList();
            var searchResults = new Mock<ISearchResults>();
            searchResults.Setup(x => x.GetEnumerator()).Returns(() => results.GetEnumerator());
            _query.Setup(x => x.Execute(It.IsAny<QueryOptions>())).Returns(searchResults.Object);
        }

        private static IMember CreateMember(string? propertyValue)
        {
            var member = new Mock<IMember>();
            member.Setup(x => x.GetValue<string>(PROPERTY_ALIAS, null, null, false)).Returns(propertyValue);
            return member.Object;
        }

        [Fact]
        public void Returns_members_whose_property_exactly_matches_ignoring_case()
        {
            SetupSearchResults(1, 2, 3);
            var exactMatch = CreateMember("someone@example.org");
            var differentCase = CreateMember("Someone@Example.org");
            var similarButDifferent = CreateMember("someone@example.org.uk");
            _memberService.Setup(x => x.GetAllMembers(1, 2, 3)).Returns([exactMatch, differentCase, similarButDifferent]);

            var result = new ExamineMemberPropertySearch(_examineManager.Object, _memberService.Object).FindMembersByPropertyValue(PROPERTY_ALIAS, "someone@example.org");

            Assert.Equal(2, result.Count());
            Assert.Contains(exactMatch, result);
            Assert.Contains(differentCase, result);
        }

        [Fact]
        public void Returns_no_members_when_the_index_has_no_matches()
        {
            SetupSearchResults();

            var result = new ExamineMemberPropertySearch(_examineManager.Object, _memberService.Object).FindMembersByPropertyValue(PROPERTY_ALIAS, "someone@example.org");

            Assert.Empty(result);
            _memberService.Verify(x => x.GetAllMembers(It.IsAny<int[]>()), Times.Never);
        }

        [Fact]
        public void Returns_no_members_for_an_empty_value_without_searching()
        {
            var result = new ExamineMemberPropertySearch(_examineManager.Object, _memberService.Object).FindMembersByPropertyValue(PROPERTY_ALIAS, string.Empty);

            Assert.Empty(result);
            _query.Verify(x => x.Execute(It.IsAny<QueryOptions>()), Times.Never);
        }

        [Fact]
        public void Throws_if_the_members_index_is_missing()
        {
            IIndex? missingIndex = null;
            _examineManager.Setup(x => x.TryGetIndex(UmbracoIndexes.MembersIndexName, out missingIndex)).Returns(false);

            Assert.Throws<InvalidOperationException>(() => new ExamineMemberPropertySearch(_examineManager.Object, _memberService.Object).FindMembersByPropertyValue(PROPERTY_ALIAS, "someone@example.org"));
        }
    }
}
