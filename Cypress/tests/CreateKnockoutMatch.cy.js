import { logToConsole } from "./functions/logging";

describe("Create knockout match", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit(
        "/competitions/surrey-ladies-stoolball-association-knockout-cup/2021/matches/add/knockout"
      );
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
