describe("Edit season", () => {
  it("Requires authentication", () => {
    cy.visit("/competitions/mid-sussex-mixed-league/2021/edit/season");
    cy.contains("Sign in");
  });
});
