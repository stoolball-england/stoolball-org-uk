import { html, css, customElement, LitElement, property, state, nothing } from '@umbraco-cms/backoffice/external/lit';
import type { PropertyValues } from '@umbraco-cms/backoffice/external/lit';
import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
import type { UmbBlockEditorCustomViewElement, UmbBlockEditorCustomViewConfiguration } from '@umbraco-cms/backoffice/block-custom-view';
import type { UmbBlockDataType } from '@umbraco-cms/backoffice/block';
import { getBoolean, getFirstMediaKey } from '../helpers/value-helper';
import { getPreviewImage, type BlockPreviewImage } from '../helpers/media-helper';
import { blockPreviewStyles, siteStylesheet } from '../helpers/styles';

const MAX_IMAGE_SIZE = 800;

interface ImageContent extends UmbBlockDataType {
    image?: unknown;
    showCaption?: boolean | string;
}

@customElement('stoolball-image')
export class StoolballImageView extends UmbElementMixin(LitElement) implements UmbBlockEditorCustomViewElement {

    @property({ attribute: false })
    content?: ImageContent;

    @property({ attribute: false })
    config?: UmbBlockEditorCustomViewConfiguration;

    @state()
    private _image?: BlockPreviewImage;

    #mediaKey?: string;

    static override styles = [blockPreviewStyles, css`
        /* Shrink the figure to the width of the image, so that the caption is the same width as the image */
        figure.block__images {
            width: fit-content;
            max-width: 100%;
            margin: 0;
        }
        /* Limit the preview to 800px wide or high, keeping the aspect ratio. The selector is more specific than
           the site's '.block__images img[src]', which sets the width to 100%. */
        figure.block__images img[src] {
            width: auto;
            height: auto;
            max-width: min(100%, ${MAX_IMAGE_SIZE}px);
            max-height: ${MAX_IMAGE_SIZE}px;
        }
    `];

    override async willUpdate(changedProperties: PropertyValues) {
        super.willUpdate(changedProperties);
        if (!changedProperties.has('content')) return;

        const mediaKey = getFirstMediaKey(this.content?.image);
        if (mediaKey === this.#mediaKey) return;
        this.#mediaKey = mediaKey;
        this._image = mediaKey ? await getPreviewImage(this, mediaKey, MAX_IMAGE_SIZE) : undefined;
    }

    override render() {
        return html`${siteStylesheet}
        <a href="${this.config?.editContentPath ?? ''}" class="block-preview">
            ${this._image ? html`<figure class="block__images">
                <img src="${this._image.url}" alt="${this._image.name}" />
                ${getBoolean(this.content?.showCaption) ? html`<figcaption>${this._image.name}</figcaption>` : nothing}
            </figure>` : html`<p>No image selected</p>`}
        </a>`;
    }
}

export default StoolballImageView;
