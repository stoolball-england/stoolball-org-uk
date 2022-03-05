import { logToConsole } from "./functions/logging";

describe("Create match location", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/locations/add");
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
