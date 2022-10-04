import { logToConsole } from "./functions/logging";

describe("Match location", () => {
  beforeEach(() => {
    cy.visit("/locations/maresfield-recreation-ground");
    cy.injectAxe();
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.checkA11y(null, null, logToConsole);
  });
});
