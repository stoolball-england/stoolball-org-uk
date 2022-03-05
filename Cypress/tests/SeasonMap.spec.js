import { logToConsole } from "./functions/logging";

describe("Season map", () => {
  beforeEach(() => {
    cy.visit("/competitions/mid-sussex-mixed-league/2021/map");
    cy.injectAxe();
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.checkA11y(null, null, logToConsole);
  });
});
