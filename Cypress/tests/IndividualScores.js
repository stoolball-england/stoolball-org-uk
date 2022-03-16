import { logToConsole } from "./functions/logging";

describe("Highest individual scores", () => {
  beforeEach(() => {
    cy.visit("/play/statistics/individual-scores");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.injectAxe();
    cy.checkA11y(null, null, logToConsole);
  });
});
