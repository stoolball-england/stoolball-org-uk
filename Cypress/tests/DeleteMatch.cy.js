import { logToConsole } from "./functions/logging";

describe("Delete match", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/matches/maresfield-mixed-new-school-ninjas-22jul2021/delete");
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
