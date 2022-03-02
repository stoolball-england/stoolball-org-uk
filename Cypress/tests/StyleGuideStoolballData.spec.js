describe("Style guide - stoolball data section", () => {
  beforeEach(() => {
    cy.visit("/style-guide/?alttemplate=styleguidestoolballdata");
  });

  it("Validates", () => {
    cy.htmlvalidate({
      rules: {
        "input-missing-label": "off", // not smart enough to realise fields are labelled by table headers
      },
    });
  });
});
