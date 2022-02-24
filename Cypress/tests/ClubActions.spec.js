describe("Club actions", () => {
  it("Requires authentication", () => {
    cy.visit("/clubs/maresfield/edit");
    cy.contains("Sign in");
  });
});
