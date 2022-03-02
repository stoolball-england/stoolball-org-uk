describe("Create club", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/clubs/add");
    });

    it("Validates", () => {
      cy.htmlvalidate();
    });

    it("Requires authentication", () => {
      cy.contains("Sign in");
    });
  });
});
