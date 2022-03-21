describe("CSV export", () => {
  it("Should return onlysport.csv without a key", () => {
    cy.request("/data/onlysport.csv");
  });

  it("Should return 404 for spogo.csv without a key", () => {
    cy.request({ url: "/data/spogo.csv", failOnStatusCode: false }).should(
      (response) => {
        expect(response.status).to.eq(404);
      }
    );
  });

  it("Should return 404 for spogo.csv without an invalid key", () => {
    cy.request({
      url: "/data/spogo.csv?key=invalid",
      failOnStatusCode: false,
    }).should((response) => {
      expect(response.status).to.eq(404);
    });
  });

  it("Should return spogo.csv with a valid key", () => {
    cy.request("/data/spogo.csv?key=" + Cypress.env("CsvExportApiKey"));
  });
});
