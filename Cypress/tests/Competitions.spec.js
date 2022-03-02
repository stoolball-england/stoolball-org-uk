describe("Leagues and competitions", () => {
  beforeEach(() => {
    cy.visit("/competitions");
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  // Note: This only works in Chromium as at Cypress 9.5.0. Firefox does not submit the form with the {enter} key.
  it("Searches when Enter is pressed", () => {
    let totalResultsBeforeSearch;
    cy.get(".list-results__title").should(($itemsBefore) => {
      totalResultsBeforeSearch = $itemsBefore.length;
      expect(totalResultsBeforeSearch).to.be.greaterThan(0);
    });
    cy.get("#competition-search").type("england{enter}");
    cy.location("search").should("equal", "?q=england");
    cy.get(".list-results__title").should(($itemsAfter) => {
      const totalResultsAfterSearch = $itemsAfter.length;
      expect(totalResultsAfterSearch).to.be.lessThan(totalResultsBeforeSearch);
    });
  });

  it("Searches when the button is clicked", () => {
    let totalResultsBeforeSearch;
    cy.get(".list-results__title").should(($itemsBefore) => {
      totalResultsBeforeSearch = $itemsBefore.length;
      expect(totalResultsBeforeSearch).to.be.greaterThan(0);
    });
    cy.get("#competition-search").type("england");
    cy.get(".form-search button").click();
    cy.location("search").should("equal", "?q=england");
    cy.get(".list-results__title").should(($itemsAfter) => {
      const totalResultsAfterSearch = $itemsAfter.length;
      expect(totalResultsAfterSearch).to.be.lessThan(totalResultsBeforeSearch);
    });
  });
});
