import { logToConsole } from "./functions/logging";

describe("Edit start of play", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit(
        "/matches/maresfield-mixed-education-12jun2014/edit/start-of-play"
      );
    });

    it("Validates", () => {
      cy.htmlvalidate();
    });

    it("Passes AXE", () => {
      cy.injectAxe();
      cy.checkA11y(null, null, logToConsole);
    });

    it("Requires authentication", () => {
      cy.contains("Sign in");
    });
  });
});
