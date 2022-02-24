describe("Competition actions", () => {
  it("Requires authentication", () => {
    cy.visit("/competitions/mid-sussex-mixed-league/edit");
    cy.contains("Sign in");
  });
});
