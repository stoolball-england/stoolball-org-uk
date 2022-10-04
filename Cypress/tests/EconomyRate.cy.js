import { logToConsole } from "./functions/logging";

describe("Best economy rate", () => {
  beforeEach(() => {
    cy.visit("/play/statistics/economy-rate");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.injectAxe();
    cy.checkA11y(null, null, logToConsole);
  });
});
