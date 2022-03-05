import { logToConsole } from "./functions/logging";

describe("Club", () => {
  beforeEach(() => {
    cy.visit("/clubs/maresfield");
    cy.injectAxe();
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.checkA11y(null, null, logToConsole);
  });
});
