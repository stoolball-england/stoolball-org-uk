import { logToConsole } from "./functions/logging";

describe("Best bowling strike rate", () => {
  beforeEach(() => {
    cy.visit("/play/statistics/bowling-strike-rate");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.injectAxe();
    cy.checkA11y(null, null, logToConsole);
  });
});
