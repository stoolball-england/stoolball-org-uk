describe("Matches for season", () => {
  beforeEach(() => {
    cy.visit("/competitions/mid-sussex-mixed-league/2021/matches");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });
});
