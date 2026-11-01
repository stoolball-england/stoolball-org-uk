import { expect, test } from "@playwright/test";
import {
  expectValidHtml,
  expectNoAccessibilityViolations,
} from "../support/assertions";

test.describe("Style guide - forms section", { tag: "@Production" }, () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/style-guide/?alttemplate=styleguideforms");
  });

  test("Validates", async ({ page }) => {
    await expectValidHtml(page, {
      rules: {
        "prefer-native-element": "off", // picks up TinyMCE
        "no-deprecated-attr": "off", // picks up TinyMCE
        "input-missing-label": "off", // not smart enough to recognise aria-label
        "form-dup-name": "off", // Umbraco Forms checkboxes need a hidden "false" input with the same name after the checkbox, because the first value posted is saved
      },
    });
  });

  test("TinyMCE keeps links as typed rather than converting them to relative URLs", async ({
    page,
  }) => {
    const links = [
      "https://example.org/page1/page2",
      "/page1/page2",
      "../page2",
    ];

    for (const href of links) {
      const content = await page.evaluate((href) => {
        const editor = (window as any).tinymce.get("rich-text");
        editor.setContent(`<p><a href="${href}">link</a></p>`);
        return editor.getContent();
      }, href);

      expect(content).toBe(`<p><a href="${href}">link</a></p>`);
    }
  });

  test("Passes AXE", async ({ page }) => {
    await expectNoAccessibilityViolations(
      page,
      [
        "aria-allowed-role", // picks up TinyMCE
        "button-name", // picks up TinyMCE
        "nested-interactive", // picks up TinyMCE
        "presentation-role-conflict", // picks up TinyMCE
      ],
      [
        "iframe.tox-edit-area__iframe", // TinyMCE's editable area puts aria-label on <body>, which axe flags (aria-prohibited-attr)
      ],
    );
  });
});
