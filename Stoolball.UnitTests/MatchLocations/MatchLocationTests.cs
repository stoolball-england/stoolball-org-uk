using System;
using System.Linq;
using System.Text;
using Stoolball.MatchLocations;
using Stoolball.Testing;
using Xunit;

namespace Stoolball.UnitTests.MatchLocations
{
    public class MatchLocationTests : ValidationBaseTest
    {
        [Fact]
        public void PrimaryAddressableObjectName_is_required()
        {
            var matchLocation = new MatchLocation();

            Assert.Contains(ValidateModel(matchLocation),
                v => v.MemberNames.Contains(nameof(MatchLocation.PrimaryAddressableObjectName)) &&
                     v.ErrorMessage.Contains("is required", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Town_is_required()
        {
            var matchLocation = new MatchLocation();

            Assert.Contains(ValidateModel(matchLocation),
                v => v.MemberNames.Contains(nameof(MatchLocation.Town)) &&
                     v.ErrorMessage.Contains("is required", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void PrimaryAddressableObjectName_allows_100_characters()
        {
            var x = "x";
            var someValue = new StringBuilder();
            for (var i = 1; i <= 100; i++) someValue.Append(x);

            var matchLocation = new MatchLocation { PrimaryAddressableObjectName = someValue.ToString() };

            Assert.DoesNotContain(ValidateModel(matchLocation),
                v => v.MemberNames.Contains(nameof(MatchLocation.PrimaryAddressableObjectName)) &&
                     v.ErrorMessage.Contains("maximum length", StringComparison.OrdinalIgnoreCase));
        }


        [Fact]
        public void PrimaryAddressableObjectName_disallows_101_characters()
        {
            var x = "x";
            var someValue = new StringBuilder();
            for (var i = 1; i <= 101; i++) someValue.Append(x);

            var matchLocation = new MatchLocation { PrimaryAddressableObjectName = someValue.ToString() };

            Assert.Contains(ValidateModel(matchLocation),
                v => v.MemberNames.Contains(nameof(MatchLocation.PrimaryAddressableObjectName)) &&
                     v.ErrorMessage.Contains("maximum length", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void SecondaryAddressableObjectName_allows_100_characters()
        {
            var x = "x";
            var someValue = new StringBuilder();
            for (var i = 1; i <= 100; i++) someValue.Append(x);

            var matchLocation = new MatchLocation { SecondaryAddressableObjectName = someValue.ToString() };

            Assert.DoesNotContain(ValidateModel(matchLocation),
                v => v.MemberNames.Contains(nameof(MatchLocation.SecondaryAddressableObjectName)) &&
                     v.ErrorMessage.Contains("maximum length", StringComparison.OrdinalIgnoreCase));
        }


        [Fact]
        public void SecondaryAddressableObjectName_disallows_101_characters()
        {
            var x = "x";
            var someValue = new StringBuilder();
            for (var i = 1; i <= 101; i++) someValue.Append(x);

            var matchLocation = new MatchLocation { SecondaryAddressableObjectName = someValue.ToString() };

            Assert.Contains(ValidateModel(matchLocation),
                v => v.MemberNames.Contains(nameof(MatchLocation.SecondaryAddressableObjectName)) &&
                     v.ErrorMessage.Contains("maximum length", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void StreetDescription_allows_100_characters()
        {
            var x = "x";
            var someValue = new StringBuilder();
            for (var i = 1; i <= 100; i++) someValue.Append(x);

            var matchLocation = new MatchLocation { StreetDescription = someValue.ToString() };

            Assert.DoesNotContain(ValidateModel(matchLocation),
                v => v.MemberNames.Contains(nameof(MatchLocation.StreetDescription)) &&
                     v.ErrorMessage.Contains("maximum length", StringComparison.OrdinalIgnoreCase));
        }


        [Fact]
        public void StreetDescription_disallows_101_characters()
        {
            var x = "x";
            var someValue = new StringBuilder();
            for (var i = 1; i <= 101; i++) someValue.Append(x);

            var matchLocation = new MatchLocation { StreetDescription = someValue.ToString() };

            Assert.Contains(ValidateModel(matchLocation),
                v => v.MemberNames.Contains(nameof(MatchLocation.StreetDescription)) &&
                     v.ErrorMessage.Contains("maximum length", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Locality_allows_35_characters()
        {
            var x = "x";
            var someValue = new StringBuilder();
            for (var i = 1; i <= 35; i++) someValue.Append(x);

            var matchLocation = new MatchLocation { Locality = someValue.ToString() };

            Assert.DoesNotContain(ValidateModel(matchLocation),
                v => v.MemberNames.Contains(nameof(MatchLocation.Locality)) &&
                     v.ErrorMessage.Contains("maximum length", StringComparison.OrdinalIgnoreCase));
        }


        [Fact]
        public void Locality_disallows_36_characters()
        {
            var x = "x";
            var someValue = new StringBuilder();
            for (var i = 1; i <= 36; i++) someValue.Append(x);

            var matchLocation = new MatchLocation { Locality = someValue.ToString() };

            Assert.Contains(ValidateModel(matchLocation),
                v => v.MemberNames.Contains(nameof(MatchLocation.Locality)) &&
                     v.ErrorMessage.Contains("maximum length", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Town_allows_30_characters()
        {
            var x = "x";
            var someValue = new StringBuilder();
            for (var i = 1; i <= 30; i++) someValue.Append(x);

            var matchLocation = new MatchLocation { Town = someValue.ToString() };

            Assert.DoesNotContain(ValidateModel(matchLocation),
                v => v.MemberNames.Contains(nameof(MatchLocation.Town)) &&
                     v.ErrorMessage.Contains("maximum length", StringComparison.OrdinalIgnoreCase));
        }


        [Fact]
        public void Town_disallows_31_characters()
        {
            var x = "x";
            var someValue = new StringBuilder();
            for (var i = 1; i <= 31; i++) someValue.Append(x);

            var matchLocation = new MatchLocation { Town = someValue.ToString() };

            Assert.Contains(ValidateModel(matchLocation),
                v => v.MemberNames.Contains(nameof(MatchLocation.Town)) &&
                     v.ErrorMessage.Contains("maximum length", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void AdministrativeArea_allows_30_characters()
        {
            var x = "x";
            var someValue = new StringBuilder();
            for (var i = 1; i <= 30; i++) someValue.Append(x);

            var matchLocation = new MatchLocation { AdministrativeArea = someValue.ToString() };

            Assert.DoesNotContain(ValidateModel(matchLocation),
                v => v.MemberNames.Contains(nameof(MatchLocation.AdministrativeArea)) &&
                     v.ErrorMessage.Contains("maximum length", StringComparison.OrdinalIgnoreCase));
        }


        [Fact]
        public void AdministrativeArea_disallows_31_characters()
        {
            var x = "x";
            var someValue = new StringBuilder();
            for (var i = 1; i <= 31; i++) someValue.Append(x);

            var matchLocation = new MatchLocation { AdministrativeArea = someValue.ToString() };

            Assert.Contains(ValidateModel(matchLocation),
                v => v.MemberNames.Contains(nameof(MatchLocation.AdministrativeArea)) &&
                     v.ErrorMessage.Contains("maximum length", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Postcode_allows_9_characters()
        {
            var x = "x";
            var someValue = new StringBuilder();
            for (var i = 1; i <= 9; i++) someValue.Append(x);

            var matchLocation = new MatchLocation { Postcode = someValue.ToString() };

            Assert.DoesNotContain(ValidateModel(matchLocation),
                v => v.MemberNames.Contains(nameof(MatchLocation.Postcode)) &&
                     v.ErrorMessage.Contains("maximum length", StringComparison.OrdinalIgnoreCase));
        }


        [Fact]
        public void Postcode_disallows_10_characters()
        {
            var x = "x";
            var someValue = new StringBuilder();
            for (var i = 1; i <= 10; i++) someValue.Append(x);

            var matchLocation = new MatchLocation { Postcode = someValue.ToString() };

            Assert.Contains(ValidateModel(matchLocation),
                v => v.MemberNames.Contains(nameof(MatchLocation.Postcode)) &&
                     v.ErrorMessage.Contains("maximum length", StringComparison.OrdinalIgnoreCase));
        }
    }
}
