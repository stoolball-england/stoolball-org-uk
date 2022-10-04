import { logToConsole } from "./functions/logging";

describe("Catches", () => {
  beforeEach(() => {
    cy.visit("/players/rick-mason/catches");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.injectAxe();
    cy.checkA11y(null, null, logToConsole);
  });
});
