import { logToConsole } from "./functions/logging";

describe("Match actions", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/matches/maresfield-mixed-brook-street-10apr2021/edit");
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
