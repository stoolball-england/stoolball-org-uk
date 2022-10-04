import { logToConsole } from "./functions/logging";

describe("Edit tournament teams", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/tournaments/slsa-tournament-23jun2013/edit/teams");
    });

    it("Validates", () => {
      cy.htmlvalidate();
    });

    it("Passes AXE", () => {
      cy.injectAxe();
      cy.checkA11y(null, null, logToConsole);
    });

    it("Requires authentication", () => {
      cy.contains("Sign in");
    });
  });
});
