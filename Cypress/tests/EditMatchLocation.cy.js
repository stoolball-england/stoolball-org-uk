import { logToConsole } from "./functions/logging";

describe("Edit match location", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/locations/maresfield-recreation-ground/edit/location");
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
