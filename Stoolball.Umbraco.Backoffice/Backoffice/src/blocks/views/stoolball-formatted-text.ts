import { html, customElement, LitElement, property, unsafeHTML } from '@umbraco-cms/backoffice/external/lit';
import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
import type { UmbBlockEditorCustomViewElement, UmbBlockEditorCustomViewConfiguration } from '@umbraco-cms/backoffice/block-custom-view';
import type { UmbBlockDataType } from '@umbraco-cms/backoffice/block';
import type { UmbPropertyEditorRteValueType } from '@umbraco-cms/backoffice/rte';
import { disableLinks } from '../helpers/html-helper';
import { getRichTextMarkup } from '../helpers/value-helper';
import { blockPreviewStyles, siteStylesheet } from '../helpers/styles';

interface FormattedTextContent extends UmbBlockDataType {
    text?: UmbPropertyEditorRteValueType | string;
}

@customElement('stoolball-formatted-text')
export class StoolballFormattedTextView extends UmbElementMixin(LitElement) implements UmbBlockEditorCustomViewElement {

    @property({ attribute: false })
    content?: FormattedTextContent;

    @property({ attribute: false })
    config?: UmbBlockEditorCustomViewConfiguration;

    static override styles = [blockPreviewStyles];

    override render() {
        return html`${siteStylesheet}
        <a href="${this.config?.editContentPath ?? ''}" class="block-preview">${unsafeHTML(disableLinks(getRichTextMarkup(this.content?.text)))}</a>`;
    }
}

export default StoolballFormattedTextView;
