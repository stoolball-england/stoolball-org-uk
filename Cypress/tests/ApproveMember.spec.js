import { logToConsole } from "./functions/logging";

describe("Activate member", () => {
  describe("Without a key", () => {
    beforeEach(() => {
      cy.visit("/account/activate/");
      cy.injectAxe();
    });

    it("Validates", () => {
      cy.htmlvalidate();
    });

    it("Passes AXE", () => {
      cy.checkA11y(null, null, logToConsole);
    });

    it("Does not activate a member", () => {
      cy.contains("Sorry, we weren't able to activate your account.");
    });
  });
});
