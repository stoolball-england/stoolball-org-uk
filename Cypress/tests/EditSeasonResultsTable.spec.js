describe("Edit season results table", () => {
  it("Requires authentication", () => {
    cy.visit("/competitions/mid-sussex-mixed-league/2021/edit/table");
    cy.contains("Sign in");
  });
});
