describe("Logout member", () => {
  describe("When signed out", () => {
    beforeEach(() => {
      cy.visit("/account/sign-out/");
    });

    it("Validates", () => {
      cy.htmlvalidate();
    });
  });
});
