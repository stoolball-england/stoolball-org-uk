import { logToConsole } from "./functions/logging";

describe("Product list", () => {
  beforeEach(() => {
    cy.visit("/shop");
    cy.injectAxe();
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.checkA11y(null, null, logToConsole);
  });
});
