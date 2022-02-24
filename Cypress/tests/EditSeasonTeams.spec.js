describe("Edit season teams", () => {
  it("Requires authentication", () => {
    cy.visit("/competitions/mid-sussex-mixed-league/2021/edit/teams");
    cy.contains("Sign in");
  });
});
