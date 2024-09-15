import { logToConsole } from "./functions/logging";

describe("Match", () => {
  beforeEach(() => {
    cy.visit("/matches/maresfield-mixed-new-school-ninjas-22jul2021");
    cy.injectAxe();
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.checkA11y(null, null, logToConsole);
  });
});
