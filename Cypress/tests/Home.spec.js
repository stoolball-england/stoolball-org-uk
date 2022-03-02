describe("Home page", () => {
  beforeEach(() => {
    cy.visit("/");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Links to 'what is stoolball?'", () => {
    cy.contains("Read more about stoolball").click();
    cy.url().should("include", "/rules/what-is-stoolball");
  });
});
