import { logToConsole } from "./functions/logging";

describe("Player batting", () => {
  beforeEach(() => {
    cy.visit("/players/rick-mason");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.injectAxe();
    cy.checkA11y(null, null, logToConsole);
  });
});
