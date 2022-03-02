describe("Team statistics", () => {
  beforeEach(() => {
    cy.visit("/teams/maresfield-mixed/statistics");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });
});
