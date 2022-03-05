describe("Matches for match location", () => {
  beforeEach(() => {
    cy.visit("/locations/maresfield-recreation-ground/matches");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });
});
