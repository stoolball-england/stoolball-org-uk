describe("Club", () => {
  beforeEach(() => {
    cy.visit("/clubs/maresfield");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });
});
