import { html, customElement, LitElement, property, unsafeHTML } from '@umbraco-cms/backoffice/external/lit';
import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
import type { UmbBlockEditorCustomViewElement, UmbBlockEditorCustomViewConfiguration } from '@umbraco-cms/backoffice/block-custom-view';
import type { UmbBlockDataType } from '@umbraco-cms/backoffice/block';
import type { UmbPropertyEditorRteValueType } from '@umbraco-cms/backoffice/rte';
import { disableLinks, stripTagsExceptInlineFormatting } from '../helpers/html-helper';
import { getRichTextMarkup } from '../helpers/value-helper';
import { blockPreviewStyles, siteStylesheet } from '../helpers/styles';

interface TipContent extends UmbBlockDataType {
    text?: UmbPropertyEditorRteValueType | string;
}

@customElement('stoolball-tip')
export class StoolballTipView extends UmbElementMixin(LitElement) implements UmbBlockEditorCustomViewElement {

    @property({ attribute: false })
    content?: TipContent;

    @property({ attribute: false })
    config?: UmbBlockEditorCustomViewConfiguration;

    static override styles = [blockPreviewStyles];

    override render() {
        const text = disableLinks(stripTagsExceptInlineFormatting(getRichTextMarkup(this.content?.text)));
        return html`${siteStylesheet}
        <a href="${this.config?.editContentPath ?? ''}" class="block-preview">
            <p class="alert alert-info alert-tip"><span>${unsafeHTML(text)}</span></p>
        </a>`;
    }
}

export default StoolballTipView;
