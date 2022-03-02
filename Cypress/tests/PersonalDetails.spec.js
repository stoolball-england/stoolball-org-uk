describe("Edit email address", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/account/personal-details/");
    });

    it("Validates", () => {
      cy.htmlvalidate();
    });

    it("Requires authentication", () => {
      cy.contains("Sign in");
    });
  });
});
