import { logToConsole } from "./functions/logging";

describe("Best bowling figures", () => {
  beforeEach(() => {
    cy.visit("/play/statistics/bowling-figures");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.injectAxe();
    cy.checkA11y(null, null, logToConsole);
  });
});
