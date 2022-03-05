import { logToConsole } from "./functions/logging";

describe("Home page", () => {
  beforeEach(() => {
    cy.visit("/");
    cy.injectAxe();
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.checkA11y(null, null, logToConsole);
  });

  it("Links to 'what is stoolball?'", () => {
    cy.contains("Read more about stoolball").click();
    cy.url().should("include", "/rules/what-is-stoolball");
  });
});
