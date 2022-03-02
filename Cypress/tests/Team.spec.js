describe("Team", () => {
  beforeEach(() => {
    cy.visit("/teams/maresfield-mixed");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });
});
