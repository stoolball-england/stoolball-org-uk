describe("Edit email address", () => {
  it("Requires authentication", () => {
    cy.visit("/account/email/");
    cy.contains("Sign in");
  });
});
