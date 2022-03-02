describe("Clubs and teams map", () => {
  beforeEach(() => {
    cy.visit("/teams/map");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });
});
