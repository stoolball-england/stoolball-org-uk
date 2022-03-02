describe("Season statistics", () => {
  beforeEach(() => {
    cy.visit("/competitions/mid-sussex-mixed-league/2021/statistics");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });
});
