using System;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;

namespace Stoolball.Umbraco.Backoffice.PropertyEditors
{
    public class StylesheetPickerPropertyValueConverter : PropertyValueConverterBase
    {
        /// <remarks>
        /// Since Umbraco 14 the data type uses the built-in <c>Umbraco.Plain.String</c> schema, so match on the property editor UI registered in Backoffice/src/property-editors/manifests.ts.
        /// </remarks>
        public override bool IsConverter(IPublishedPropertyType propertyType) => propertyType?.EditorUiAlias?.Equals("Stoolball.StylesheetPicker") ?? false;

        /// <inheritdoc />
        public override Type GetPropertyValueType(IPublishedPropertyType propertyType) => typeof(string);

        /// <inheritdoc />
        public override object? ConvertSourceToIntermediate(
            IPublishedElement owner,
            IPublishedPropertyType propertyType,
            object? source,
            bool preview) =>
            source?.ToString();
    }
}
