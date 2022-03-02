describe("Competition statistics", () => {
  beforeEach(() => {
    cy.visit("/competitions/mid-sussex-mixed-league/statistics");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });
});
