using System;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.PropertyEditors;

#pragma warning disable CA1707 // Identifiers should not contain underscores
namespace Stoolball.Web.App_Plugins.Stoolball.PropertyEditors
#pragma warning restore CA1707 // Identifiers should not contain underscores
{
    public class StylesheetPickerPropertyValueConverter : PropertyValueConverterBase
    {
        public override bool IsConverter(IPublishedPropertyType propertyType) => propertyType?.EditorAlias.Equals("Stoolball.StylesheetPicker") ?? false;

        /// <inheritdoc />
        public override Type GetPropertyValueType(IPublishedPropertyType propertyType) => typeof(string);

        /// <inheritdoc />
        public override object ConvertSourceToIntermediate(
            IPublishedElement owner,
            IPublishedPropertyType propertyType,
            object source,
            bool preview) =>
            source?.ToString();
    }
}
