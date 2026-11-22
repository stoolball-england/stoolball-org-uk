import { html, css, customElement, property, state } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { UmbChangeEvent } from '@umbraco-cms/backoffice/event';
import type { UmbPropertyEditorUiElement } from '@umbraco-cms/backoffice/property-editor';
import type { UmbPropertyEditorConfigCollection } from '@umbraco-cms/backoffice/property-editor';
import { UmbStylesheetTreeRepository } from '@umbraco-cms/backoffice/stylesheet';

/**
 * Picks a stylesheet from the root of the stylesheets folder (wwwroot/css). The value is the name of the stylesheet
 * without its .css extension, for example "rules.page", so that the page can load the .min.css version.
 */
@customElement('stoolball-stylesheet-picker')
export class StoolballStylesheetPickerElement extends UmbLitElement implements UmbPropertyEditorUiElement {

    @property()
    value?: string;

    @property({ type: Boolean, reflect: true })
    readonly = false;

    @property({ attribute: false })
    public set config(config: UmbPropertyEditorConfigCollection | undefined) {
        this.#filter = config?.getValueByAlias<string>('filter');
        this.#applyFilter();
    }

    @state()
    private _stylesheets: Array<string> = [];

    #treeRepository = new UmbStylesheetTreeRepository(this);
    #filter?: string;
    #allStylesheets: Array<string> = [];

    static override styles = css`
        uui-select {
            min-width: 300px;
        }
    `;

    override async connectedCallback() {
        super.connectedCallback();
        const { data } = await this.#treeRepository.requestTreeRootItems({ skip: 0, take: 1000 });
        this.#allStylesheets = (data?.items ?? [])
            .filter((item) => !item.isFolder)
            .map((item) => item.name.replace(/\.css$/i, ''));
        this.#applyFilter();
    }

    #applyFilter() {
        if (this.#filter) {
            const regex = new RegExp(this.#filter);
            this._stylesheets = this.#allStylesheets.filter((name) => regex.test(name));
        } else {
            this._stylesheets = [...this.#allStylesheets];
        }
    }

    #onChange(event: Event) {
        this.value = (event.target as HTMLSelectElement).value;
        this.dispatchEvent(new UmbChangeEvent());
    }

    override render() {
        const options = [
            { name: '', value: '', selected: !this.value },
            ...this._stylesheets.map((name) => ({ name, value: name, selected: name === this.value })),
        ];
        return html`<uui-select
            label="Stylesheet"
            .options=${options}
            ?readonly=${this.readonly}
            ?disabled=${this.readonly}
            @change=${this.#onChange}></uui-select>`;
    }
}

export default StoolballStylesheetPickerElement;
