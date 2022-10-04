import { logToConsole } from "./functions/logging";

describe("Link player to member", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/players/rick-mason/link-to-my-account");
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
