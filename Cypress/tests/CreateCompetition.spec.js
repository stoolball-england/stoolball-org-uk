describe("Create competition", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/competitions/add");
    });

    it("Validates", () => {
      cy.htmlvalidate();
    });

    it("Requires authentication", () => {
      cy.contains("Sign in");
    });
  });
});
