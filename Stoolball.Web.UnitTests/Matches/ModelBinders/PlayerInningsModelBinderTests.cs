using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using Moq;
using Stoolball.Matches;
using Stoolball.Web.Matches.ModelBinders;
using Stoolball.Web.Matches.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches.ModelBinders
{
    public class PlayerInningsModelBinderTests
    {

        [Fact]
        public async Task Caught_by_bowler_is_converted_to_caught_and_bowled()
        {
            var modelName = "Field";
            var valueProvider = new Mock<IValueProvider>();
            valueProvider.Setup(x => x.GetValue($"{modelName}.{nameof(PlayerInningsViewModel.Batter)}")).Returns(new ValueProviderResult(new StringValues("Jo Bloggs")));
            valueProvider.Setup(x => x.GetValue($"{modelName}.{nameof(PlayerInningsViewModel.DismissalType)}")).Returns(new ValueProviderResult(new StringValues(nameof(DismissalType.Caught))));
            valueProvider.Setup(x => x.GetValue($"{modelName}.{nameof(PlayerInningsViewModel.DismissedBy)}")).Returns(new ValueProviderResult(new StringValues("John Smith")));
            valueProvider.Setup(x => x.GetValue($"{modelName}.{nameof(PlayerInningsViewModel.Bowler)}")).Returns(new ValueProviderResult(new StringValues("John Smith")));

            var binder = new PlayerInningsModelBinder();
            var context = new DefaultModelBindingContext
            {
                ModelName = modelName,
                ModelState = new ModelStateDictionary(),
                ValueProvider = valueProvider.Object,
            };


            await binder.BindModelAsync(context);
            var result = (PlayerInningsViewModel)context.Result.Model;


            Assert.Equal(DismissalType.CaughtAndBowled, result.DismissalType);
            Assert.True(string.IsNullOrEmpty(result.DismissedBy));
            Assert.False(string.IsNullOrEmpty(result.Bowler));
        }
    }
}
