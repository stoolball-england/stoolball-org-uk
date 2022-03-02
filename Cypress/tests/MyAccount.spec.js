describe("My account", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/account");
    });

    it("Validates", () => {
      cy.htmlvalidate();
    });

    it("Requires authentication", () => {
      cy.contains("Sign in");
    });
  });
});
