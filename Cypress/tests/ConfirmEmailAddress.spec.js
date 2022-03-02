describe("Confirm email address", () => {
  describe("Without a key", () => {
    beforeEach(() => {
      cy.visit("/account/confirm-email/");
    });

    it("Validates", () => {
      cy.htmlvalidate();
    });

    it("Does not confirm the email address", () => {
      cy.contains(
        "Sorry, your request to change your email address could not be confirmed."
      );
    });
  });
});
