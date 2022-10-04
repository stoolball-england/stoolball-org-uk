import { logToConsole } from "./functions/logging";

describe("Transient team", () => {
  beforeEach(() => {
    cy.visit(
      "/tournaments/lewes-arms-tournament-7jul2013/teams/brighton-beachcombers/edit"
    );
    cy.injectAxe();
  });

  it("Passes AXE", () => {
    cy.checkA11y(null, null, logToConsole);
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });
});
