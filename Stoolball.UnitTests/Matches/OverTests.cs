using System;
using System.Linq;
using Stoolball.Matches;
using Stoolball.Testing;
using Xunit;

namespace Stoolball.UnitTests.Matches
{
    public class OverTests : ValidationBaseTest
    {
        [Theory]
        [InlineData(0)]
        [InlineData(13)]
        public void Invalid_BallsBowled_fails_validation(int ballsBowled)
        {
            var over = new Over
            {
                BallsBowled = ballsBowled
            };

            Assert.Contains(ValidateModel(over),
                v => v.MemberNames.Contains(nameof(Over.BallsBowled)) &&
                     (v.ErrorMessage?.Contains("balls bowled", StringComparison.OrdinalIgnoreCase) ?? false));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(1)]
        [InlineData(12)]
        public void Valid_BallsBowled_passes_validation(int? ballsBowled)
        {
            var over = new Over
            {
                BallsBowled = ballsBowled
            };

            Assert.DoesNotContain(ValidateModel(over),
                            v => v.MemberNames.Contains(nameof(Over.BallsBowled)) &&
                                 (v.ErrorMessage?.Contains("balls bowled", StringComparison.OrdinalIgnoreCase) ?? false));
        }

        [Fact]
        public void Negative_Wides_fails_validation()
        {
            var over = new Over
            {
                Wides = -1
            };

            Assert.Contains(ValidateModel(over),
                v => v.MemberNames.Contains(nameof(Over.Wides)) &&
                     (v.ErrorMessage?.Contains("wides", StringComparison.OrdinalIgnoreCase) ?? false));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(0)]
        public void Valid_Wides_passes_validation(int? wides)
        {
            var over = new Over
            {
                Wides = wides
            };

            Assert.DoesNotContain(ValidateModel(over),
                            v => v.MemberNames.Contains(nameof(Over.Wides)) &&
                                 (v.ErrorMessage?.Contains("wides", StringComparison.OrdinalIgnoreCase) ?? false));
        }

        [Fact]
        public void Negative_NoBalls_fails_validation()
        {
            var over = new Over
            {
                NoBalls = -1
            };

            Assert.Contains(ValidateModel(over),
                v => v.MemberNames.Contains(nameof(Over.NoBalls)) &&
                     (v.ErrorMessage?.Contains("no balls", StringComparison.OrdinalIgnoreCase) ?? false));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(0)]
        public void Valid_NoBalls_passes_validation(int? noBalls)
        {
            var over = new Over
            {
                NoBalls = noBalls
            };

            Assert.DoesNotContain(ValidateModel(over),
                            v => v.MemberNames.Contains(nameof(Over.NoBalls)) &&
                                 (v.ErrorMessage?.Contains("no balls", StringComparison.OrdinalIgnoreCase) ?? false));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(0)]
        [InlineData(-10)]
        public void Valid_RunsConceded_passes_validation(int? runs)
        {
            var over = new Over
            {
                RunsConceded = runs
            };

            Assert.DoesNotContain(ValidateModel(over),
                            v => v.MemberNames.Contains(nameof(Over.RunsConceded)) &&
                                 (v.ErrorMessage?.Contains("over total", StringComparison.OrdinalIgnoreCase) ?? false));
        }
    }
}
