export const manifests: Array<UmbExtensionManifest> = [
    {
        type: 'propertyEditorUi',
        alias: 'Stoolball.StylesheetPicker',
        name: 'Stylesheet Picker Property Editor UI',
        element: () => import('./views/stoolball-stylesheet-picker'),
        elementName: 'stoolball-stylesheet-picker',
        meta: {
            label: 'Stylesheet Picker',
            propertyEditorSchemaAlias: 'Umbraco.Plain.String',
            icon: 'icon-paint-roller',
            group: 'pickers',
            settings: {
                properties: [
                    {
                        alias: 'filter',
                        label: 'Filename filter',
                        description: 'A regular expression which stylesheets must match to be included',
                        propertyEditorUiAlias: 'Umb.PropertyEditorUi.TextBox'
                    }
                ]
            }
        }
    }
];
