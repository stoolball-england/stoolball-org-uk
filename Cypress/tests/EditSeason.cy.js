import { logToConsole } from "./functions/logging";

describe("Edit season", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/competitions/mid-sussex-mixed-league/2021/edit/season");
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
