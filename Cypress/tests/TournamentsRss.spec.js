describe("Tournaments RSS feed", () => {
  describe("In standard format", () => {
    it("Loads", () => {
      cy.request("/tournaments.rss");
    });
  });
  describe("In Twitter format", () => {
    it("Loads", () => {
      cy.request("/tournaments.rss?format=tweet");
    });
  });
});
