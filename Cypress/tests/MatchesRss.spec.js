describe("Matches RSS feed", () => {
  describe("In standard format", () => {
    it("Loads", () => {
      cy.request("/matches.rss");
    });
  });
  describe("In Twitter format", () => {
    it("Loads", () => {
      cy.request("/matches.rss?format=tweet");
    });
  });
});
