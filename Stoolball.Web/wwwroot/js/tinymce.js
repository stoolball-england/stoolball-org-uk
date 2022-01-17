window.addEventListener("DOMContentLoaded", function () {
  // URLs are specified for plugins, theme and skin because it makes them work when bundled by ClientDependency
  tinymce.PluginManager.load(
    "link",
    "/umbraco/lib/tinymce/plugins/link/plugin.min.js"
  );
  tinymce.PluginManager.load(
    "lists",
    "/umbraco/lib/tinymce/plugins/lists/plugin.min.js"
  );
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

  // When you tab to a TinyMCE area it announces the title of the iframe, but that's not the field label,
  // so try to update it. This all becomes obsolete in TinyMCE 5.9.0 when you can set a property.
  // https://github.com/tinymce/tinymce/pull/7017
  const updateLabel = function (textareaId) {
    // Find the TinyMCE iframe. If it hasn't loaded, wait and try again.
    const iframe = document.getElementById(textareaId + "_ifr");
    if (!iframe) {
      setTimeout(function () {
        updateLabel(textareaId), 500;
      });
      return;
    }

    // Get any labels linked from a label element or by aria-decribedby.
    let labels = Array.prototype.slice.call(
      document.querySelectorAll("label[for='" + textareaId + "']"),
      0
    );
    let describedBySelector = document
      .getElementById(textareaId)
      .getAttribute("aria-describedby")
      .replace(/\s+/, ",#");
    if (describedBySelector) {
      labels = labels.concat(
        Array.prototype.slice.call(
          document.querySelectorAll("#" + describedBySelector)
        )
      );
    }

    if (!labels.length) {
      return;
    }

    // Prepend their textContent to the existing title attribute of the iframe.
    iframe.setAttribute(
      "title",
      finishWithFullStop(
        labels
          .map(function (element) {
            return element.textContent.trim();
          })
          .join(". ")
      ) + iframe.getAttribute("title")
    );
  };

  const finishWithFullStop = function (text) {
    while (text && text.match(/[\.| ]$/)) {
      text = text.substring(0, text.length - 1);
    }
    return (text && text + ". ") || "";
  };

  // Trigger that process for the same selector that was hooked up to TinyMCE
  const textareas = document.querySelectorAll("textarea");
  for (let i = 0; i < textareas.length; i++) {
    if (textareas[i].id) {
      updateLabel(textareas[i].id);
    }
  }
});
