describe("Create match location", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/locations/add");
    });

    it("Validates", () => {
      cy.htmlvalidate();
    });

    it("Requires authentication", () => {
      cy.contains("Sign in");
    });
  });
});
