describe("Confirm email address", () => {
  describe("Without a key", () => {
    it("Is invalid", () => {
      cy.visit("/account/confirm-email/");
      cy.contains(
        "Sorry, your request to change your email address could not be confirmed."
      );
    });
  });
});
