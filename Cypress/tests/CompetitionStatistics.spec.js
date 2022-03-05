import { logToConsole } from "./functions/logging";

describe("Competition statistics", () => {
  beforeEach(() => {
    cy.visit("/competitions/mid-sussex-mixed-league/statistics");
    cy.injectAxe();
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.checkA11y(null, null, logToConsole);
  });
});
