import { logToConsole } from "./functions/logging";

describe("Match location actions", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/locations/maresfield-recreation-ground/edit");
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
