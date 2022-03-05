describe("Create team", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/teams/add");
    });

    it("Validates", () => {
      cy.htmlvalidate();
    });

    it("Requires authentication", () => {
      cy.contains("Sign in");
    });
  });
});
