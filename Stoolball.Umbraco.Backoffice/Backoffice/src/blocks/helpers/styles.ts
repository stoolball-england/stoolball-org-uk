import { css, html } from '@umbraco-cms/backoffice/external/lit';

/**
 * Loads the site's base stylesheet into the shadow DOM of a block preview, so that it looks like the block on the site.
 */
export const siteStylesheet = html`<link rel="stylesheet" href="/css/base.min.css" />`;

/**
 * Makes the whole block preview a link to edit the block, without it being styled as a link.
 *
 * The site's stylesheet styles a:visited, a:hover and a:focus, which are more specific than a class alone,
 * so every link state is listed to override its colour, underline and outline.
 */
export const blockPreviewStyles = css`
    :host {
        display: block;
    }
    .block-preview,
    a.block-preview:is(:link, :visited, :hover, :focus, :active) {
        display: block;
        color: inherit;
        text-decoration: none;
        padding: var(--uui-size-space-3, 9px) var(--uui-size-space-4, 12px);
        border: 1px solid var(--uui-color-border, #d8d7d9);
        border-radius: var(--uui-border-radius, 3px);
        background: #fff;
        outline: none;
    }
    a.block-preview:is(:hover, :focus) {
        background: #f1f8eb; /* very pale tint of the site's light green, #b5d498 */
    }
    a.block-preview:focus-visible {
        outline: 2px solid var(--uui-color-focus, #3879ff);
        outline-offset: 2px;
    }
    .block-preview img,
    .block-preview iframe {
        max-width: 100%;
    }
`;
