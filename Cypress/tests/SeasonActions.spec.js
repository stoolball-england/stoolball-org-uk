describe("Season actions", () => {
  it("Requires authentication", () => {
    cy.visit("/competitions/mid-sussex-mixed-league/2021/edit");
    cy.contains("Sign in");
  });
});
