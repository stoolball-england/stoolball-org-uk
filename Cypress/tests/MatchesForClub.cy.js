import { logToConsole } from "./functions/logging";

describe("Matches for club", () => {
  beforeEach(() => {
    cy.visit("/clubs/maresfield/matches");
    cy.injectAxe();
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.checkA11y(null, null, logToConsole);
  });
});
