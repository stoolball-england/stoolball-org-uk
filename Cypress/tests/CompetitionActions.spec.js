import { logToConsole } from "./functions/logging";

describe("Competition actions", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/competitions/mid-sussex-mixed-league/edit");
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
