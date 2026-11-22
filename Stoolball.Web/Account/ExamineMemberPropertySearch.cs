using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Examine;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Examine;
using static Umbraco.Cms.Core.Constants;

namespace Stoolball.Web.Account
{
    /// <summary>
    /// Finds members by the value of a property using the Examine members index, which Umbraco recommends
    /// in place of <c>IMemberService.GetMembersByPropertyValue</c>.
    /// </summary>
    public class ExamineMemberPropertySearch : IMemberPropertySearch
    {
        private readonly IExamineManager _examineManager;
        private readonly IMemberService _memberService;

        public ExamineMemberPropertySearch(IExamineManager examineManager, IMemberService memberService)
        {
            _examineManager = examineManager ?? throw new ArgumentNullException(nameof(examineManager));
            _memberService = memberService ?? throw new ArgumentNullException(nameof(memberService));
        }

        /// <inheritdoc />
        public IEnumerable<IMember> FindMembersByPropertyValue(string propertyAlias, string value)
        {
            if (string.IsNullOrWhiteSpace(propertyAlias)) { throw new ArgumentException("A property alias is required", nameof(propertyAlias)); }
            if (string.IsNullOrEmpty(value)) { return []; }

            if (!_examineManager.TryGetIndex(UmbracoIndexes.MembersIndexName, out var index))
            {
                throw new InvalidOperationException($"The Examine index {UmbracoIndexes.MembersIndexName} was not found");
            }

            var results = index.Searcher.CreateQuery(IndexTypes.Member).Field(propertyAlias, value.Escape()).Execute();
            var memberIds = results.Select(result => int.Parse(result.Id, CultureInfo.InvariantCulture)).ToArray();
            if (memberIds.Length == 0) { return []; }

            // The index analyses the value into search terms, so check the member data for an exact match
            return _memberService.GetAllMembers(memberIds)
                .Where(member => string.Equals(member.GetValue<string>(propertyAlias), value, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }
}
