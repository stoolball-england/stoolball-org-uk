import { logToConsole } from "./functions/logging";

describe("Style guide", () => {
  beforeEach(() => {
    cy.visit("/style-guide");
    cy.injectAxe();
  });

  it("Validates", () => {
    cy.htmlvalidate({ rules: { "wcag/h32": "off" } }); // h32 is a technique, not a success criterion. Consent checkboxes are active immediately, which is a different technique.
  });

  it("Passes AXE", () => {
    cy.checkA11y(null, null, logToConsole);
  });
});
