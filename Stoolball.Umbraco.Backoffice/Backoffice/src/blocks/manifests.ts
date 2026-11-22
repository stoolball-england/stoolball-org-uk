export const manifests: Array<UmbExtensionManifest> = [
    {
        type: 'blockEditorCustomView',
        alias: 'Stoolball.BlockEditorViews.FormattedText',
        name: "Block editor custom view for 'Formatted text' blocks",
        element: () => import('./views/stoolball-formatted-text'),
        forContentTypeAlias: 'formattedText'
    },
    {
        type: 'blockEditorCustomView',
        alias: 'Stoolball.BlockEditorViews.FormattedTextWithImage',
        name: "Block editor custom view for 'Formatted text with image' blocks",
        element: () => import('./views/stoolball-formatted-text-with-image'),
        forContentTypeAlias: 'FormattedTextWithImage'
    },
    {
        type: 'blockEditorCustomView',
        alias: 'Stoolball.BlockEditorViews.Image',
        name: "Block editor custom view for 'Image' blocks",
        element: () => import('./views/stoolball-image'),
        forContentTypeAlias: 'imageElementType'
    },
    {
        type: 'blockEditorCustomView',
        alias: 'Stoolball.BlockEditorViews.Tip',
        name: "Block editor custom view for 'Tip' blocks",
        element: () => import('./views/stoolball-tip'),
        forContentTypeAlias: 'tip'
    },
    {
        type: 'blockEditorCustomView',
        alias: 'Stoolball.BlockEditorViews.YouTubeVideo',
        name: "Block editor custom view for 'YouTube video' blocks",
        element: () => import('./views/stoolball-youtube-video'),
        forContentTypeAlias: 'youTubeVideo'
    },
    {
        type: 'blockEditorCustomView',
        alias: 'Stoolball.BlockEditorViews.GettyImage',
        name: "Block editor custom view for 'Getty image' blocks",
        element: () => import('./views/stoolball-getty-image'),
        forContentTypeAlias: 'gettyImage'
    }
];
