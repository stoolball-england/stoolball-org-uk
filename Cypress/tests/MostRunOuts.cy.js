import { logToConsole } from "./functions/logging";

describe("Most run-outs", () => {
  beforeEach(() => {
    cy.visit("/play/statistics/most-run-outs");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.injectAxe();
    cy.checkA11y(null, null, logToConsole);
  });
});
