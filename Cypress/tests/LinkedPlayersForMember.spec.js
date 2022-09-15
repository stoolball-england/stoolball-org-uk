import { logToConsole } from "./functions/logging";

describe("Linked players for member", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/account/linked-players");
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
