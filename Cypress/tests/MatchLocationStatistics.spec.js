import { logToConsole } from "./functions/logging";

describe("Match location statistics", () => {
  beforeEach(() => {
    cy.visit("/locations/maresfield-recreation-ground/statistics");
    cy.injectAxe();
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.checkA11y(null, null, logToConsole);
  });
});
