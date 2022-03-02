describe("Season actions", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/competitions/mid-sussex-mixed-league/2021/edit");
    });

    it("Validates", () => {
      cy.htmlvalidate();
    });

    it("Requires authentication", () => {
      cy.contains("Sign in");
    });
  });
});
