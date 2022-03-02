describe("News story", () => {
  beforeEach(() => {
    cy.visit("/news/minutes-of-the-41st-annual-general-meeting/");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });
});
