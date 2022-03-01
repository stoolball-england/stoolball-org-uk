describe("Team actions", () => {
  it("Requires authentication", () => {
    cy.visit("/teams/maresfield-mixed/edit");
    cy.contains("Sign in");
  });
});
