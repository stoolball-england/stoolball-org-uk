describe("Create competition", () => {
  it("Requires authentication", () => {
    cy.visit("/competitions/add");
    cy.contains("Sign in");
  });
});
