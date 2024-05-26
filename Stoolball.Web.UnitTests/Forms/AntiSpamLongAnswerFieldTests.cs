using Stoolball.Web.App_Plugins.Stoolball.UmbracoForms;
using Umbraco.Forms.Core.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Forms
{
    public class AntiSpamLongAnswerFieldTests
    {
        [Theory]
        [InlineData(@"Hi - I run the example ladies and mixed teams but can't login to edit our Info

thankyou")]
        [InlineData(@"My team is in Sussex and that shouldn't be blocked.")]
        public void ValidMessageShouldPass(string message)
        {
            var field = new AntiSpamLongAnswerField();

            var result = field.ValidateField(new Form(), new Field(), new[] { message }, null!, null!);

            Assert.Empty(result);
        }

        [Theory]
        [InlineData(@"video chat porn")]
        [InlineData(@"Hi guys! Especially for you, we have selected the best trance music - listen for free here: https://example.org")]
        [InlineData(@"private sex video")]
        [InlineData(@"Крун-10 https://example.org/ktp_kru ")]
        [InlineData(@"Доброго")]
        [InlineData(@"CLICK HERE https://example.org")]
        public void InvalidMessageShouldNotPass(string message)
        {
            var field = new AntiSpamLongAnswerField();

            var result = field.ValidateField(new Form(), new Field(), new[] { message }, null!, null!);

            Assert.NotEmpty(result);
        }
    }
}
