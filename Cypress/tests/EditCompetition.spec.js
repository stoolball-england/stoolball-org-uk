describe("Edit competition", () => {
  it("Requires authentication", () => {
    cy.visit("/competitions/mid-sussex-mixed-league/edit/competition");
    cy.contains("Sign in");
  });
});
