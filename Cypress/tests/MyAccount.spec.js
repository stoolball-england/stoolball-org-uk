describe("My account", () => {
  it("Requires authentication", () => {
    cy.visit("/account");
    cy.contains("Sign in");
  });
});
