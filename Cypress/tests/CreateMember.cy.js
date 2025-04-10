import { logToConsole } from "./functions/logging";

describe("Create competition", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/account/create");
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
