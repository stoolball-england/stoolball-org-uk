import { logToConsole } from "./functions/logging";

describe("Tournaments", () => {
  beforeEach(() => {
    cy.visit("/tournaments");
    cy.injectAxe();
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.checkA11y(null, null, logToConsole);
  });
});
