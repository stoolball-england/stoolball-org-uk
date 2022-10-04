import { logToConsole } from "./functions/logging";

describe("Matches for season", () => {
  beforeEach(() => {
    cy.visit("/competitions/mid-sussex-mixed-league/2021/matches");
    cy.injectAxe();
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.checkA11y(null, null, logToConsole);
  });
});
