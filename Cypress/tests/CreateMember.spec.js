describe("Create competition", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/account/create");
    });

    it("Validates", () => {
      cy.htmlvalidate();
    });

    it("Requires authentication", () => {
      cy.contains("Sign in");
    });
  });
});
