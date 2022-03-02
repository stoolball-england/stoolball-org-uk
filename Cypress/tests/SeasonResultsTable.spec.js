describe("Season results table", () => {
  beforeEach(() => {
    cy.visit("/competitions/mid-sussex-mixed-league/2021/table");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });
});
