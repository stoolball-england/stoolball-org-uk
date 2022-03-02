describe("Transient team", () => {
  beforeEach(() => {
    cy.visit(
      "/tournaments/lewes-arms-tournament-7jul2013/teams/brighton-beachcombers"
    );
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });
});
