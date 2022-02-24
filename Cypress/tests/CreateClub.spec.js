describe("Create club", () => {
  it("Requires authentication", () => {
    cy.visit("/clubs/add");
    cy.contains("Sign in");
  });
});
