import { logToConsole } from "./functions/logging";

describe("Team", () => {
  beforeEach(() => {
    cy.visit("/teams/maresfield-mixed");
    cy.injectAxe();
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.checkA11y(null, null, logToConsole);
  });
});
