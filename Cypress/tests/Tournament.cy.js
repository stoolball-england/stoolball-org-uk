import { logToConsole } from "./functions/logging";

describe("Tournament", () => {
  beforeEach(() => {
    cy.visit("/tournaments/slsa-tournament-23jun2013");
    cy.injectAxe();
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.checkA11y(null, null, logToConsole);
  });
});
