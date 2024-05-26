using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Umbraco.Forms.Core.Enums;
using Umbraco.Forms.Core.Models;
using Umbraco.Forms.Core.Services;

namespace Stoolball.Web.App_Plugins.Stoolball.UmbracoForms
{
    public class AntiSpamLongAnswerField : Umbraco.Forms.Core.FieldType
    {
        private const string _spammyWords = @"\b(PORN|PRON|PORNSTAR|SEX|SEXY|WEBSEX|SEXCHAT|SEXCAM|NAKED|NUDE|NUDES|SEXCAMS|TRANCE|SEO|CLICK HERE|ROBOT)\b";
        private const string _cyrillicCharacters = "(К|Д)";

        public AntiSpamLongAnswerField()
        {
            Id = new Guid("49eefb95-94b7-44d1-9124-5dbedd02f55d");
            Name = "Long answer (anti-spam)";
            Description = "Renders a textarea, designed for longer answers.";
            Icon = "icon-autofill";
            DataType = FieldDataType.LongString;
            SortOrder = 10;
            SupportsRegex = false;
            FieldTypeViewName = "FieldType.Textarea.cshtml";
        }

        public override string GetDesignView() => "~/App_Plugins/UmbracoForms/backoffice/Common/FieldTypes/textarea.html";

        public override IEnumerable<string> ValidateField(Form form, Field field, IEnumerable<object> postedValues, HttpContext context, IPlaceholderParsingService placeholderParsingService)
        {
            var errors = new List<string>();
            var normalisedPostedValues = postedValues.Select(x => (x.ToString() ?? "").ToUpperInvariant().Replace(Environment.NewLine, " "));

            if (normalisedPostedValues.Any(value => Regex.IsMatch(value, _spammyWords, RegexOptions.Multiline)) ||
                normalisedPostedValues.Any(value => Regex.IsMatch(value, _cyrillicCharacters, RegexOptions.Multiline)))
            {
                errors.Add("Your message has been blocked due to its content.");
            }

            // Also validate it against the default method (to handle mandatory fields and regular expressions)
            return base.ValidateField(form, field, postedValues, context, placeholderParsingService, errors);
        }
    }
}

