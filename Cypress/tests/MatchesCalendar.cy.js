describe("Matches calendar", () => {
  it("Loads", () => {
    cy.request("/matches.ics");
  });
});
