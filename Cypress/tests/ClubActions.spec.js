describe("Club actions", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/clubs/maresfield/edit");
    });

    it("Validates", () => {
      cy.htmlvalidate();
    });

    it("Requires authentication", () => {
      cy.contains("Sign in");
    });
  });
});
