import { logToConsole } from "./functions/logging";

describe("News story", () => {
  beforeEach(() => {
    cy.visit("/news/minutes-of-the-41st-annual-general-meeting/");
    cy.injectAxe();
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.checkA11y(null, null, logToConsole);
  });
});
