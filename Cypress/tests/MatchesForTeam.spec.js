describe("Matches for team", () => {
  beforeEach(() => {
    cy.visit("/teams/maresfield-mixed/matches");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });
});
