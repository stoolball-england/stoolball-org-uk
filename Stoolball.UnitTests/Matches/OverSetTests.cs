using System;
using System.Linq;
using Stoolball.Matches;
using Stoolball.Testing;
using Xunit;

namespace Stoolball.UnitTests.Matches
{
    public class OverSetTests : ValidationBaseTest
    {
        [Fact]
        public void Invalid_Overs_fails_validation()
        {
            var overSet = new OverSet
            {
                Overs = 0
            };

            Assert.Contains(ValidateModel(overSet),
                v => v.MemberNames.Contains(nameof(OverSet.Overs)) &&
                     (v.ErrorMessage?.Contains("at least 1", StringComparison.OrdinalIgnoreCase) ?? false));
        }
    }
}
