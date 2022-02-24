describe("Edit club", () => {
  it("Requires authentication", () => {
    cy.visit("/clubs/maresfield/edit/club");
    cy.contains("Sign in");
  });
});
