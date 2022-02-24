describe("Login member", () => {
  it("Requires authentication", () => {
    cy.visit("/account/sign-in/");
    cy.contains("Sign in");
  });
});
