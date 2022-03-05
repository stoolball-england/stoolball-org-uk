import { logToConsole } from "./functions/logging";

describe("Competition", () => {
  beforeEach(() => {
    cy.visit("/competitions/mid-sussex-mixed-league");
    cy.injectAxe();
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.checkA11y(null, null, logToConsole);
  });
});
