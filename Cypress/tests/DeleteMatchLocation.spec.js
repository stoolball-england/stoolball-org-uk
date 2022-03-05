describe("Delete match location", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/locations/maresfield-recreation-ground/delete");
    });

    it("Validates", () => {
      cy.htmlvalidate();
    });

    it("Requires authentication", () => {
      cy.contains("Sign in");
    });
  });
});
