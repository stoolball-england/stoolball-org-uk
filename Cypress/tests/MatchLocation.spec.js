describe("Match location", () => {
  beforeEach(() => {
    cy.visit("/locations/maresfield-recreation-ground");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });
});
