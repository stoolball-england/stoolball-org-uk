describe("Delete club", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/clubs/maresfield/delete");
    });

    it("Validates", () => {
      cy.htmlvalidate();
    });

    it("Requires authentication", () => {
      cy.contains("Sign in");
    });
  });
});
