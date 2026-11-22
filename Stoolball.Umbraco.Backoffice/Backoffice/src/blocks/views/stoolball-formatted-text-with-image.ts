import { html, css, customElement, LitElement, property, state, nothing, unsafeHTML } from '@umbraco-cms/backoffice/external/lit';
import type { PropertyValues } from '@umbraco-cms/backoffice/external/lit';
import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
import type { UmbBlockEditorCustomViewElement, UmbBlockEditorCustomViewConfiguration } from '@umbraco-cms/backoffice/block-custom-view';
import type { UmbBlockDataType } from '@umbraco-cms/backoffice/block';
import type { UmbPropertyEditorRteValueType } from '@umbraco-cms/backoffice/rte';
import { disableLinks } from '../helpers/html-helper';
import { getBoolean, getFirstMediaKey, getRichTextMarkup, getSelectedValue } from '../helpers/value-helper';
import { getPreviewImage, type BlockPreviewImage } from '../helpers/media-helper';
import { blockPreviewStyles, siteStylesheet } from '../helpers/styles';

interface FormattedTextWithImageContent extends UmbBlockDataType {
    text?: UmbPropertyEditorRteValueType | string;
    image?: unknown;
    imagePosition?: string | string[];
    showCaption?: boolean | string;
}

@customElement('stoolball-formatted-text-with-image')
export class StoolballFormattedTextWithImageView extends UmbElementMixin(LitElement) implements UmbBlockEditorCustomViewElement {

    @property({ attribute: false })
    content?: FormattedTextWithImageContent;

    @property({ attribute: false })
    config?: UmbBlockEditorCustomViewConfiguration;

    @state()
    private _image?: BlockPreviewImage;

    #mediaKey?: string;

    // The site positions the image using breakpoints based on the browser window, which don't work for a preview
    // inside the back office, so the preview lays out the text and image itself.
    static override styles = [blockPreviewStyles, css`
        .block__text-with-image {
            display: flex;
            align-items: flex-start;
            gap: 2rem;
        }
        .block__text {
            flex: 1 1 66.6%;
            min-width: 0;
        }
        .block__image {
            flex: 0 1 33.3%;
            margin: 0;
        }
        .block__image img {
            display: block;
            max-width: 100%;
            height: auto;
        }
        .block__text-with-image__left .block__image {
            order: 1;
        }
        .block__text-with-image__left .block__text {
            order: 2;
        }
    `];

    override async willUpdate(changedProperties: PropertyValues) {
        super.willUpdate(changedProperties);
        if (!changedProperties.has('content')) return;

        const mediaKey = getFirstMediaKey(this.content?.image);
        if (mediaKey === this.#mediaKey) return;
        this.#mediaKey = mediaKey;
        this._image = mediaKey ? await getPreviewImage(this, mediaKey) : undefined;
    }

    override render() {
        const imagePosition = (getSelectedValue(this.content?.imagePosition) ?? 'left').toLowerCase();
        return html`${siteStylesheet}
        <a href="${this.config?.editContentPath ?? ''}" class="block-preview">
            <div class="block__text-with-image block__text-with-image__${imagePosition}">
                <div class="block__text">${unsafeHTML(disableLinks(getRichTextMarkup(this.content?.text)))}</div>
                ${this._image ? html`<figure class="block__image">
                    <img src="${this._image.url}" alt="${this._image.name}" />
                    ${getBoolean(this.content?.showCaption) ? html`<figcaption>${this._image.name}</figcaption>` : nothing}
                </figure>` : nothing}
            </div>
        </a>`;
    }
}

export default StoolballFormattedTextWithImageView;
