describe("Season map", () => {
  beforeEach(() => {
    cy.visit("/competitions/mid-sussex-mixed-league/2021/map");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });
});
