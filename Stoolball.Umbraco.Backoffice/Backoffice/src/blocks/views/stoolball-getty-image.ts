import { html, customElement, LitElement, property } from '@umbraco-cms/backoffice/external/lit';
import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
import type { UmbBlockEditorCustomViewElement, UmbBlockEditorCustomViewConfiguration } from '@umbraco-cms/backoffice/block-custom-view';
import type { UmbBlockDataType } from '@umbraco-cms/backoffice/block';
import { blockPreviewStyles } from '../helpers/styles';

interface GettyImageContent extends UmbBlockDataType {
    gettyImageURL?: string;
    caption?: string;
}

@customElement('stoolball-getty-image')
export class StoolballGettyImageView extends UmbElementMixin(LitElement) implements UmbBlockEditorCustomViewElement {

    @property({ attribute: false })
    content?: GettyImageContent;

    @property({ attribute: false })
    config?: UmbBlockEditorCustomViewConfiguration;

    static override styles = [blockPreviewStyles];

    override render() {
        // This is not a link to edit the block because an iframe cannot be inside a link. Editors use the block's edit button instead.
        return html`<div class="block-preview">
            <figure>
                ${this.content?.gettyImageURL
                    ? html`<iframe src="${this.content.gettyImageURL}" title="${this.content.caption || 'Getty image'}" width="594" height="459" frameborder="0" scrolling="no"></iframe>`
                    : html`<p>No Getty image URL</p>`}
                <figcaption>${this.content?.caption ?? ''}</figcaption>
            </figure>
        </div>`;
    }
}

export default StoolballGettyImageView;
