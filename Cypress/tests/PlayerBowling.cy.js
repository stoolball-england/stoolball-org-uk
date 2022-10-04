import { logToConsole } from "./functions/logging";

describe("Player bowling", () => {
  beforeEach(() => {
    cy.visit("/players/rick-mason/bowling");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.injectAxe();
    cy.checkA11y(null, null, logToConsole);
  });
});
