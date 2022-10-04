import { logToConsole } from "./functions/logging";

describe("Run-outs", () => {
  beforeEach(() => {
    cy.visit("/players/rick-mason/run-outs");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.injectAxe();
    cy.checkA11y(null, null, logToConsole);
  });
});
