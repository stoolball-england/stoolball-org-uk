describe("Competition", () => {
  beforeEach(() => {
    cy.visit("/competitions/mid-sussex-mixed-league");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });
});
