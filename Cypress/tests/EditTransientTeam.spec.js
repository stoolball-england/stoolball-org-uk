import { logToConsole } from "./functions/logging";

describe("Edit transient team", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/teams/maresfield-mixed/edit/team");
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
