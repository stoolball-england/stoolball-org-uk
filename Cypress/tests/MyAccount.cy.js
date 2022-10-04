import { logToConsole } from "./functions/logging";

describe("My account", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/account");
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
