describe("Style guide", () => {
  beforeEach(() => {
    cy.visit("/style-guide");
  });

  it("Validates", () => {
    cy.htmlvalidate({ rules: { "wcag/h32": "off" } }); // h32 is a technique, not a success criterion. Consent checkboxes are active immediately, which is a different technique.
  });
});
