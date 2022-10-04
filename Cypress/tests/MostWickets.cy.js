import { logToConsole } from "./functions/logging";

describe("Most wickets", () => {
  beforeEach(() => {
    cy.visit("/play/statistics/most-wickets");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.injectAxe();
    cy.checkA11y(null, null, logToConsole);
  });
});
