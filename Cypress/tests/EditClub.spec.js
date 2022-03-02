describe("Edit club", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/clubs/maresfield/edit/club");
    });

    it("Validates", () => {
      cy.htmlvalidate();
    });

    it("Requires authentication", () => {
      cy.contains("Sign in");
    });
  });
});
