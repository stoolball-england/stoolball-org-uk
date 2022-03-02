describe("Style guide - forms section", () => {
  beforeEach(() => {
    cy.visit("/style-guide/?alttemplate=styleguideforms");
  });

  it("Validates", () => {
    cy.htmlvalidate({
      rules: {
        "prefer-native-element": "off", // picks up TinyMCE
        "no-deprecated-attr": "off", // picks up TinyMCE
        "input-missing-label": "off", // not smart enough to recognise aria-label
      },
    });
  });
});
