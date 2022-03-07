import { logToConsole } from "./functions/logging";

describe("Tournament actions", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/tournaments/slsa-tournament-23jun2013/edit");
      cy.injectAxe();
    });

    it("Validates", () => {
      cy.htmlvalidate();
    });

    it("Passes AXE", () => {
      cy.checkA11y(null, null, logToConsole);
    });

    it("Requires authentication", () => {
      cy.contains("Sign in");
    });
  });
});
