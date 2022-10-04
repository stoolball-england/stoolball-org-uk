import { logToConsole } from "./functions/logging";

describe("Edit statistics", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/play/statistics/edit");
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
