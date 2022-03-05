import { logToConsole } from "./functions/logging";

describe("Matches for match location", () => {
  beforeEach(() => {
    cy.visit("/locations/maresfield-recreation-ground/matches");
    cy.injectAxe();
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.checkA11y(null, null, logToConsole);
  });
});
