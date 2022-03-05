describe("Edit match location", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/locations/maresfield-recreation-ground/edit/location");
    });

    it("Validates", () => {
      cy.htmlvalidate();
    });

    it("Requires authentication", () => {
      cy.contains("Sign in");
    });
  });
});
