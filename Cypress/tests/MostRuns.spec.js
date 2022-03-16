import { logToConsole } from "./functions/logging";

describe("Most runs", () => {
  beforeEach(() => {
    cy.visit("/play/statistics/most-runs");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.injectAxe();
    cy.checkA11y(null, null, logToConsole);
  });
});
