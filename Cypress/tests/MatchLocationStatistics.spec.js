describe("Match location statistics", () => {
  beforeEach(() => {
    cy.visit("/locations/maresfield-recreation-ground/statistics");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });
});
