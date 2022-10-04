import { logToConsole } from "./functions/logging";

describe("Matches and tournaments", () => {
  beforeEach(() => {
    cy.visit("/matches");
    cy.injectAxe();
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.checkA11y(null, null, logToConsole);
  });
});
