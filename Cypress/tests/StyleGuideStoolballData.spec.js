import { logToConsole } from "./functions/logging";

describe("Style guide - stoolball data section", () => {
  beforeEach(() => {
    cy.visit("/style-guide/?alttemplate=styleguidestoolballdata");
    cy.injectAxe();
  });

  it("Validates", () => {
    cy.htmlvalidate({
      rules: {
        "input-missing-label": "off", // not smart enough to realise fields are labelled by table headers
      },
    });
  });

  it("Passes AXE", () => {
    cy.checkA11y(null, null, logToConsole);
  });
});
