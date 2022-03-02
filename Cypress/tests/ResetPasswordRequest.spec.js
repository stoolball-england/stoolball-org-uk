describe("Request password reset", () => {
  beforeEach(() => {
    cy.visit("/account/reset-password/");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });
});
