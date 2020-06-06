window.addEventListener('DOMContentLoaded', function () {
    // URLs are specified for plugins, theme and skin because it makes them work when bundled by ClientDependency
    tinymce.PluginManager.load('link', '/umbraco/lib/tinymce/plugins/link/plugin.min.js');
    tinymce.PluginManager.load('lists', '/umbraco/lib/tinymce/plugins/lists/plugin.min.js');
    tinymce.init({
        selector: "textarea",

        // General options
        theme_url: "/umbraco/lib/tinymce/themes/modern/theme.min.js",
        skin_url: "/umbraco/lib/tinymce/skins/lightgray",
        plugins: "link lists",
        statusbar: false,
        height: 300,
        menubar: false,
        toolbar: "formatselect bold italic bullist numlist link removeformat",
        block_formats: "Paragraph=p;Heading=h2",
        valid_elements: "p,h2,strong/b,em/i,ul,ol,li,a[href],br",
        link_title: false,
        target_list: false,

        // Set appearance of text within editor
        content_css: "/css/tinymce.css",
    });
});
