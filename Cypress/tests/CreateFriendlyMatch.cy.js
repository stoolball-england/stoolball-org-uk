import { logToConsole } from "./functions/logging";

describe("Create friendly match", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit(
        "/competitions/mid-sussex-mixed-league/2021/matches/add/friendly"
      );
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
