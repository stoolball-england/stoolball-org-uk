describe("Club statistics", () => {
  beforeEach(() => {
    cy.visit("/clubs/maresfield/statistics");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });
});
