/**
 * Replaces links with spans so that links in rich text are not clickable in a block preview,
 * where the whole block is already a link to edit the block.
 */
export function disableLinks(html: string | undefined): string | undefined {
    if (!html) return html;
    return html
        .replace(/<a\b([^>]*?)\s*\bhref\s*=\s*(['"])[^'"]*\2([^>]*)>/gi, '<span$1$3>')
        .replace(/<\/a>/gi, '</span>');
}

/**
 * Removes all tags except strong, em and links, matching what the site renders for a tip.
 */
export function stripTagsExceptInlineFormatting(html: string | undefined): string | undefined {
    if (!html) return html;
    return html.replace(/<(?!\/?(strong|a|em)(?=>|\s?.*>))\/?.*?>/gi, '');
}
