import type { UmbPropertyEditorRteValueType } from '@umbraco-cms/backoffice/rte';

/**
 * Gets the HTML from a rich text value. Values saved before Umbraco 15 may still be a plain HTML string
 * when they are inside a block that has not been saved since the upgrade.
 */
export function getRichTextMarkup(value: UmbPropertyEditorRteValueType | string | undefined | null): string | undefined {
    if (!value) return undefined;
    return typeof value === 'string' ? value : value.markup;
}

/**
 * Gets the key of the first media item from a media picker value. This is an array of picked items for Media Picker 3,
 * but may still be a UDI such as umb://media/0b8f... when it was saved by the legacy media picker inside a block
 * that has not been saved since the upgrade.
 */
export function getFirstMediaKey(value: unknown): string | undefined {
    if (!value) return undefined;

    if (Array.isArray(value)) {
        const mediaKey = value[0]?.mediaKey;
        return typeof mediaKey === 'string' ? mediaKey : undefined;
    }

    if (typeof value === 'string') {
        const match = /^umb:\/\/media\/([0-9a-f]{32})/i.exec(value);
        if (match) {
            const hex = match[1].toLowerCase();
            return `${hex.substring(0, 8)}-${hex.substring(8, 12)}-${hex.substring(12, 16)}-${hex.substring(16, 20)}-${hex.substring(20)}`;
        }
    }

    return undefined;
}

/**
 * Gets a true/false value, which may be a boolean or the string '1' if it was saved before the upgrade.
 */
export function getBoolean(value: unknown): boolean {
    return value === true || value === '1' || value === 1;
}

/**
 * Gets the selected value from a radio button list, which may be a string or a single-item array.
 */
export function getSelectedValue(value: unknown): string | undefined {
    if (Array.isArray(value)) return typeof value[0] === 'string' ? value[0] : undefined;
    return typeof value === 'string' ? value : undefined;
}
