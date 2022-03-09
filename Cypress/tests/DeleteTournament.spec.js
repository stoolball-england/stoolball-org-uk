import { logToConsole } from "./functions/logging";

describe("Delete tournament", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/tournaments/slsa-tournament-23jun2013/delete");
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
