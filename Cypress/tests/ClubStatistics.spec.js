import { logToConsole } from "./functions/logging";

describe("Club statistics", () => {
  beforeEach(() => {
    cy.visit("/clubs/maresfield/statistics");
    cy.injectAxe();
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.checkA11y(null, null, logToConsole);
  });
});
