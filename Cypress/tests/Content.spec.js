describe("Content page", () => {
  beforeEach(() => {
    cy.visit("/rules/what-is-stoolball");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });
});
