import { logToConsole } from "./functions/logging";

describe("Edit email address", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/account/personal-details/");
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
