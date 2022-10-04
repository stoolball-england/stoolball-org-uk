import { logToConsole } from "./functions/logging";

describe("Players for team", () => {
  beforeEach(() => {
    cy.visit("/teams/maresfield-mixed/players");
    cy.injectAxe();
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.checkA11y(null, null, logToConsole);
  });
});
