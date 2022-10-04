import { logToConsole } from "./functions/logging";

describe("Create training session", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit(
        "/competitions/mid-sussex-mixed-league/2021/matches/add/training"
      );
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
