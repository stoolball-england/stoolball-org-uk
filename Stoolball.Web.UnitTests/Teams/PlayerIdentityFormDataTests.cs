using System;
using System.Linq;
using Stoolball.Testing;
using Stoolball.Web.Teams.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Teams
{
    public class PlayerIdentityFormDataTests : ValidationBaseTest
    {
        [Fact]
        public void Name_is_required()
        {
            var data = new PlayerIdentityFormData();

            Assert.Contains(ValidateModel(data),
                v => v.MemberNames.Contains(nameof(PlayerIdentityFormData.PlayerSearch)) &&
                     (v.ErrorMessage?.Contains("is required", StringComparison.OrdinalIgnoreCase) ?? false));
        }
    }
}
