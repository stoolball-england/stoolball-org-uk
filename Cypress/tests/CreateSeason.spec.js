describe("Create season", () => {
  it("Requires authentication", () => {
    cy.visit("/competitions/mid-sussex-mixed-league/add");
    cy.contains("Sign in");
  });
});
