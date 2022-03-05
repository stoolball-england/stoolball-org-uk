import { logToConsole } from "./functions/logging";

describe("Create team", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/teams/add");
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
