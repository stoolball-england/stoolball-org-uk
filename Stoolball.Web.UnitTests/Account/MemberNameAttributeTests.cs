using System.ComponentModel.DataAnnotations;
using Stoolball.Web.Account;
using Xunit;

namespace Stoolball.Web.UnitTests.Account
{
    public class MemberNameAttributeTests
    {
        private readonly MemberNameAttribute _attribute = new MemberNameAttribute();

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Alice")]
        [InlineData("John Doe")]
        [InlineData("tron link")]
        [InlineData("Tron link")]
        [InlineData("TRON LINK")]
        [InlineData("A name with http but not a link")]
        public void IsValid_ReturnsSuccess_ForValidNames(string? name)
        {
            var result = _attribute.GetValidationResult(name, new ValidationContext(new object()));
            Assert.Equal(ValidationResult.Success, result);
        }

        [Theory]
        [InlineData("tronlink")]
        [InlineData("TRONLINK")]
        [InlineData("TronLink")]
        [InlineData("MyTronLinkAccount")]
        [InlineData("http://example.com")]
        [InlineData("https://example.com")]
        [InlineData("Visit https://mysite.com for info")]
        [InlineData("http://")]
        [InlineData("https://")]
        public void IsValid_ReturnsError_ForInvalidNames(string name)
        {
            var result = _attribute.GetValidationResult(name, new ValidationContext(new object()));
            Assert.NotEqual(ValidationResult.Success, result);
            Assert.Equal("You cannot create an account with that name", result?.ErrorMessage);
        }
    }
}
