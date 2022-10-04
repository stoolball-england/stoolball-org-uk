import { logToConsole } from "./functions/logging";

describe("Schools", () => {
  beforeEach(() => {
    cy.visit("/schools/find");
    cy.injectAxe();
  });

  it("Validates", () => {
    cy.htmlvalidate();
  });

  it("Passes AXE", () => {
    cy.checkA11y(null, null, logToConsole);
  });

  // Note: This only works in Chromium as at Cypress 9.5.0. Firefox does not submit the form with the {enter} key.
  it("Searches when Enter is pressed", () => {
    let totalResultsBeforeSearch;
    cy.get(".list-results__title").should(($itemsBefore) => {
      totalResultsBeforeSearch = $itemsBefore.length;
      expect(totalResultsBeforeSearch).to.be.greaterThan(0);
    });
    cy.get("#school-search").type("academy{enter}");
    cy.location("search").should("equal", "?q=academy");
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
    cy.get("#school-search").type("academy");
    cy.get(".form-search button").click();
    cy.location("search").should("equal", "?q=academy");
    cy.get(".list-results__title").should(($itemsAfter) => {
      const totalResultsAfterSearch = $itemsAfter.length;
      expect(totalResultsAfterSearch).to.be.lessThan(totalResultsBeforeSearch);
    });
  });
});
