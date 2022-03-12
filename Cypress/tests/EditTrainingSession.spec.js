import { logToConsole } from "./functions/logging";

describe("Edit training session", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit(
        "/matches/maresfield-mixed-maresfield-mixed-6apr2017/edit/training"
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
