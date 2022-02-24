describe("Activate member", () => {
  describe("Without a key", () => {
    it("Is invalid", () => {
      cy.visit("/account/activate/");
      cy.contains("Sorry, we weren't able to activate your account.");
    });
  });
});
