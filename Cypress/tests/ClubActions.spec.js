import { logToConsole } from "./functions/logging";

describe("Club actions", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/clubs/maresfield/edit");
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
