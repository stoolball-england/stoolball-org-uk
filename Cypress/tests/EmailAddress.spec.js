describe("Edit email address", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/account/email/");
    });

    it("Validates", () => {
      cy.htmlvalidate();
    });

    it("Requires authentication", () => {
      cy.contains("Sign in");
    });
  });
});
