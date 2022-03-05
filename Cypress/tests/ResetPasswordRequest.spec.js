import { logToConsole } from "./functions/logging";

describe("Request password reset", () => {
  beforeEach(() => {
    cy.visit("/account/reset-password/");
    cy.injectAxe();
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.checkA11y(null, null, logToConsole);
  });
});
