describe("Edit email address", () => {
  it("Requires authentication", () => {
    cy.visit("/account/personal-details/");
    cy.contains("Sign in");
  });
});
