describe("Delete team", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/teams/maresfield-mixed/delete");
    });

    it("Validates", () => {
      cy.htmlvalidate();
    });

    it("Requires authentication", () => {
      cy.contains("Sign in");
    });
  });
});
