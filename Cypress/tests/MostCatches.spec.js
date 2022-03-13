import { logToConsole } from "./functions/logging";

describe("Most catches", () => {
  beforeEach(() => {
    cy.visit("/play/statistics/most-catches");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.injectAxe();
    cy.checkA11y(null, null, logToConsole);
  });
});
