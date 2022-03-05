import { logToConsole } from "./functions/logging";

describe("Delete team", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/teams/maresfield-mixed/delete");
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
