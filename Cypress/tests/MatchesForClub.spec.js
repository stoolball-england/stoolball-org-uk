describe("Matches for club", () => {
  beforeEach(() => {
    cy.visit("/clubs/maresfield/matches");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });
});
