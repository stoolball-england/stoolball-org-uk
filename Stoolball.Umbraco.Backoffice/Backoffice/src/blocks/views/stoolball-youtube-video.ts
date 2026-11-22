import { html, customElement, LitElement, property } from '@umbraco-cms/backoffice/external/lit';
import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
import type { UmbBlockEditorCustomViewElement, UmbBlockEditorCustomViewConfiguration } from '@umbraco-cms/backoffice/block-custom-view';
import type { UmbBlockDataType } from '@umbraco-cms/backoffice/block';
import { blockPreviewStyles } from '../helpers/styles';

interface YouTubeVideoContent extends UmbBlockDataType {
    videoURL?: string;
    videoTitle?: string;
}

@customElement('stoolball-youtube-video')
export class StoolballYouTubeVideoView extends UmbElementMixin(LitElement) implements UmbBlockEditorCustomViewElement {

    @property({ attribute: false })
    content?: YouTubeVideoContent;

    @property({ attribute: false })
    config?: UmbBlockEditorCustomViewConfiguration;

    static override styles = [blockPreviewStyles];

    override render() {
        // This is not a link to edit the block because an iframe cannot be inside a link. Editors use the block's edit button instead.
        return html`<div class="block-preview">
            ${this.content?.videoURL
                ? html`<iframe width="560" height="315" src="${this.content.videoURL}" title="${this.content.videoTitle || 'YouTube video'}" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>`
                : html`<p>No video URL</p>`}
        </div>`;
    }
}

export default StoolballYouTubeVideoView;
