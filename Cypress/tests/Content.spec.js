import { logToConsole } from "./functions/logging";

describe("Content page", () => {
  beforeEach(() => {
    cy.visit("/rules/what-is-stoolball");
    cy.injectAxe();
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.checkA11y(null, null, logToConsole);
  });
});
