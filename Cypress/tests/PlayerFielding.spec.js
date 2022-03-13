import { logToConsole } from "./functions/logging";

describe("Player fielding", () => {
  beforeEach(() => {
    cy.visit("/players/rick-mason/fielding");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.injectAxe();
    cy.checkA11y(null, null, logToConsole);
  });
});
