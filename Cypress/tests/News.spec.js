describe("News", () => {
  beforeEach(() => {
    cy.visit("/news");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });
});
