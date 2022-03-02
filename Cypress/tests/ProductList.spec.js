describe("Product list", () => {
  beforeEach(() => {
    cy.visit("/shop");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });
});
