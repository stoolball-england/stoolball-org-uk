import { logToConsole } from "./functions/logging";

describe("Best batting strike rate", () => {
  beforeEach(() => {
    cy.visit("/play/statistics/batting-strike-rate");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.injectAxe();
    cy.checkA11y(null, null, logToConsole);
  });
});
