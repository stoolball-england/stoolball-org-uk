import { logToConsole } from "./functions/logging";

describe("Team statistics", () => {
  beforeEach(() => {
    cy.visit("/teams/maresfield-mixed/statistics");
    cy.injectAxe();
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.checkA11y(null, null, logToConsole);
  });
});
