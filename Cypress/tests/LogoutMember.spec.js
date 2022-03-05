import { logToConsole } from "./functions/logging";

describe("Logout member", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/account/sign-out/");
      cy.injectAxe();
    });

    it("Validates", () => {
      cy.htmlvalidate();
    });
  });

  it("Passes AXE", () => {
    cy.checkA11y(null, null, logToConsole);
  });
});
