describe("Home page", () => {
  it("Links to 'what is stoolball?'", () => {
    cy.visit("/");
    cy.contains("Read more about stoolball").click();
    cy.url().should("include", "/rules/what-is-stoolball");
  });
});
