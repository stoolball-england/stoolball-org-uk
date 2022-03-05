describe("Edit transient team", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/teams/maresfield-mixed/edit/team");
    });

    it("Validates", () => {
      cy.htmlvalidate();
    });

    it("Requires authentication", () => {
      cy.contains("Sign in");
    });
  });
});
