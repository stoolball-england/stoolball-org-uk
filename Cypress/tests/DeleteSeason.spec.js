describe("Delete season", () => {
  it("Requires authentication", () => {
    cy.visit("/competitions/mid-sussex-mixed-league/2021/delete");
    cy.contains("Sign in");
  });
});
