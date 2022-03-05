// ***********************************************************
// This example plugins/index.js can be used to load plugins
//
// You can change the location of this file or turn off loading
// the plugins file with the 'pluginsFile' configuration option.
//
// You can read more here:
// https://on.cypress.io/plugins-guide
// ***********************************************************

// This function is called when a project is opened or re-opened (e.g. due to
// the project's config changing)

const htmlvalidate = require("cypress-html-validate/dist/plugin");

module.exports = (on, config) => {
  // `on` is used to hook into various events Cypress emits
  // `config` is the resolved Cypress config
  htmlvalidate.install(on, {
    rules: {
      "script-type": "off", // because it fails Smidge-generated <script type="text/javascript"> tags for including the type
      "require-sri": "off", // because it requires SRI for local resources (even with the crossorigin setting because the html-validate project admits detection isn't good enough)
      "long-title": "off", // some competition and team names are genuinely very long!
    },
  });

  // logs AXE output https://github.com/component-driven/cypress-axe
  on("task", {
    log(message) {
      console.log(message);

      return null;
    },
    table(message) {
      console.table(message);

      return null;
    },
  });
};
