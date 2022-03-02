describe("Login member", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/account/sign-in/");
    });

    it("Validates", () => {
      cy.htmlvalidate();
    });

    it("Requires authentication", () => {
      cy.contains("Sign in");
    });
  });
});
