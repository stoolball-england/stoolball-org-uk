import { logToConsole } from "./functions/logging";

describe("Match", () => {
  beforeEach(() => {
    cy.visit("/matches/maresfield-mixed-brook-street-10apr2021");
    cy.injectAxe();
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.checkA11y(null, null, logToConsole);
  });
});
