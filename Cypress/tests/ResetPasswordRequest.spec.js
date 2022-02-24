describe("Request password reset", () => {
  it("Loads", () => {
    cy.visit("/account/reset-password/");
  });
});
