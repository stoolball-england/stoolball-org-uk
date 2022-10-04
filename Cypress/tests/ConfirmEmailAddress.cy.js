import { logToConsole } from "./functions/logging";

describe("Confirm email address", () => {
  describe("Without a key", () => {
    beforeEach(() => {
      cy.visit("/account/confirm-email/");
      cy.injectAxe();
    });

    it("Validates", () => {
      cy.htmlvalidate();
    });

    it("Passes AXE", () => {
      cy.checkA11y(null, null, logToConsole);
    });

    it("Does not confirm the email address", () => {
      cy.contains(
        "Sorry, your request to change your email address could not be confirmed."
      );
    });
  });
});
