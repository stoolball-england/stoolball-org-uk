import { logToConsole } from "./functions/logging";

describe("Statistics", () => {
  beforeEach(() => {
    cy.visit("/play/statistics");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.injectAxe();
    cy.checkA11y(null, null, logToConsole);
  });
});
