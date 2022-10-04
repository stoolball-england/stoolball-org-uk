import { logToConsole } from "./functions/logging";

describe("Best bowling average", () => {
  beforeEach(() => {
    cy.visit("/play/statistics/bowling-average");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.injectAxe();
    cy.checkA11y(null, null, logToConsole);
  });
});
