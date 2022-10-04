import { logToConsole } from "./functions/logging";

describe("Season results table", () => {
  beforeEach(() => {
    cy.visit("/competitions/mid-sussex-mixed-league/2021/table");
    cy.injectAxe();
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.checkA11y(null, null, logToConsole);
  });
});
