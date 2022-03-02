describe("Activate member", () => {
  describe("Without a key", () => {
    beforeEach(() => {
      cy.visit("/account/activate/");
    });

    it("Validates", () => {
      cy.htmlvalidate();
    });

    it("Does not activate a member", () => {
      cy.contains("Sorry, we weren't able to activate your account.");
    });
  });
});
