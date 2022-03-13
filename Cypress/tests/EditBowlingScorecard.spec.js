import { logToConsole } from "./functions/logging";

describe("Edit bowling scorecard", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit(
        "/matches/maresfield-mixed-education-12jun2014/edit/innings/1/bowling"
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
