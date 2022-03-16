import { logToConsole } from "./functions/logging";

describe("Best batting average", () => {
  beforeEach(() => {
    cy.visit("/play/statistics/batting-average");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.injectAxe();
    cy.checkA11y(null, null, logToConsole);
  });
});
