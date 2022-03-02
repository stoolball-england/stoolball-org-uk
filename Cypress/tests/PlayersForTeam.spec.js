describe("Players for team", () => {
  beforeEach(() => {
    cy.visit("/teams/maresfield-mixed/players");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });
});
