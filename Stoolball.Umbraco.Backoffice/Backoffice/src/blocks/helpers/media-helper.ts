import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { UmbImagingRepository } from '@umbraco-cms/backoffice/imaging';
import { UmbMediaItemRepository } from '@umbraco-cms/backoffice/media';
import { ImageCropModeModel } from '@umbraco-cms/backoffice/external/backend-api';

export interface BlockPreviewImage {
    url: string;
    name: string;
}

/**
 * Gets the URL of a resized version of an image, and the name of the media item which the site uses as its caption.
 * @param maxSize The maximum width and height of the image, which keeps its aspect ratio.
 */
export async function getPreviewImage(host: UmbControllerHost, mediaKey: string, maxSize: number = 500): Promise<BlockPreviewImage | undefined> {
    const imagingRepository = new UmbImagingRepository(host);
    const mediaItemRepository = new UmbMediaItemRepository(host);

    const [{ data: urls }, { data: items }] = await Promise.all([
        imagingRepository.requestResizedItems([mediaKey], { width: maxSize, height: maxSize, mode: ImageCropModeModel.MAX }),
        mediaItemRepository.requestItems([mediaKey]),
    ]);

    const url = urls?.[0]?.url;
    if (!url) return undefined;
    return { url, name: items?.[0]?.name ?? '' };
}
