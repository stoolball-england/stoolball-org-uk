﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Stoolball.Matches;
using Stoolball.Web.Matches.Models;

namespace Stoolball.Web.Matches.ModelBinders
{
    public class PlayerInningsModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            var model = new PlayerInningsViewModel();

            if (TryBindStringField(bindingContext, bindingContext.ModelName + "." + nameof(PlayerInningsViewModel.Batter), out var batterResult))
            {
                model.Batter = batterResult;
                bindingContext.Result = ModelBindingResult.Success(model);
            }
            else
            {
                return Task.CompletedTask;
            }

            model.DismissalType = BindEnumField<DismissalType>(bindingContext, bindingContext.ModelName + "." + nameof(PlayerInningsViewModel.DismissalType));
            if (TryBindStringField(bindingContext, bindingContext.ModelName + "." + nameof(PlayerInningsViewModel.DismissedBy), out var dismissedByResult))
            {
                model.DismissedBy = dismissedByResult;
            }
            if (TryBindStringField(bindingContext, bindingContext.ModelName + "." + nameof(PlayerInningsViewModel.Bowler), out var bowlerResult))
            {
                model.Bowler = bowlerResult;
            }
            model.RunsScored = BindIntField(bindingContext, bindingContext.ModelName + "." + nameof(PlayerInningsViewModel.RunsScored));
            model.BallsFaced = BindIntField(bindingContext, bindingContext.ModelName + "." + nameof(PlayerInningsViewModel.BallsFaced));

            // Caught and bowled by the same person is caught and bowled
            if (model.DismissalType == DismissalType.Caught &&
                    !string.IsNullOrEmpty(model.DismissedBy) &&
                    model.DismissedBy == model.Bowler)
            {
                model.DismissalType = DismissalType.CaughtAndBowled;
                model.DismissedBy = null;
            }

            return Task.CompletedTask;
        }

        private static T? BindEnumField<T>(ModelBindingContext bindingContext, string fieldName) where T : struct
        {
            var bindingResult = bindingContext.ValueProvider.GetValue(fieldName);
            if (bindingResult != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(fieldName, bindingResult);
                if (Enum.TryParse(typeof(T), bindingResult.FirstValue, out var value))
                {
                    return (T)value!;
                }
            }
            return null;
        }

        private static int? BindIntField(ModelBindingContext bindingContext, string fieldName)
        {
            var bindingResult = bindingContext.ValueProvider.GetValue(fieldName);
            if (bindingResult != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(fieldName, bindingResult);
                if (int.TryParse(bindingResult.FirstValue, out int value))
                {
                    return value;
                }
            }
            return null;
        }

        private static bool TryBindStringField(ModelBindingContext bindingContext, string fieldName, out string? result)
        {
            var bindingResult = bindingContext.ValueProvider.GetValue(fieldName);
            if (bindingResult != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(fieldName, bindingResult);
                result = string.IsNullOrEmpty(bindingResult.FirstValue) ? null : bindingResult.FirstValue.Trim();
                return true;
            }
            result = null;
            return false;
        }
    }
}
