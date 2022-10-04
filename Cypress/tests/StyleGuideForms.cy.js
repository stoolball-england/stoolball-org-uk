import { logToConsole } from "./functions/logging";

describe("Style guide - forms section", () => {
  beforeEach(() => {
    cy.visit("/style-guide/?alttemplate=styleguideforms");
    cy.injectAxe();
  });

  it("Validates", () => {
    cy.htmlvalidate({
      rules: {
        "prefer-native-element": "off", // picks up TinyMCE
        "no-deprecated-attr": "off", // picks up TinyMCE
        "input-missing-label": "off", // not smart enough to recognise aria-label
      },
    });
  });

  it("Passes AXE", () => {
    cy.checkA11y(
      null,
      {
        rules: {
          "aria-allowed-role": { enabled: false }, // picks up TinyMCE
          "button-name": { enabled: false }, // picks up TinyMCE
          "nested-interactive": { enabled: false }, // picks up TinyMCE
          "presentation-role-conflict": { enabled: false }, // picks up TinyMCE
        },
      },
      logToConsole
    );
  });
});
