describe("Season", () => {
  beforeEach(() => {
    cy.visit("/competitions/mid-sussex-mixed-league/2021");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });
});
