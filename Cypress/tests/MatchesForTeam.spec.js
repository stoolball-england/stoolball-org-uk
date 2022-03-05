import { logToConsole } from "./functions/logging";

describe("Matches for team", () => {
  beforeEach(() => {
    cy.visit("/teams/maresfield-mixed/matches");
    cy.injectAxe();
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.checkA11y(null, null, logToConsole);
  });
});
