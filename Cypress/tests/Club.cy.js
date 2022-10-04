import { logToConsole } from "./functions/logging";

describe("Club", () => {
  beforeEach(() => {
    cy.visit("/clubs/maresfield");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.injectAxe();
    cy.checkA11y(null, null, logToConsole);
  });
});
