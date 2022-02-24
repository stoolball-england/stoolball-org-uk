describe("Delete competition", () => {
  it("Requires authentication", () => {
    cy.visit("/competitions/mid-sussex-mixed-league/delete");
    cy.contains("Sign in");
  });
});
