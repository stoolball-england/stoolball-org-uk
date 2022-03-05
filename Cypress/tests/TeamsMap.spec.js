import { logToConsole } from "./functions/logging";

describe("Clubs and teams map", () => {
  beforeEach(() => {
    cy.visit("/teams/map");
    cy.injectAxe();
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.checkA11y(null, null, logToConsole);
  });
});
