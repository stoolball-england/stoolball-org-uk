import { logToConsole } from "./functions/logging";

describe("News", () => {
  beforeEach(() => {
    cy.visit("/news");
    cy.injectAxe();
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.checkA11y(null, null, logToConsole);
  });
});
