import { test } from "@playwright/test";
import {
  expectValidHtml,
  expectNoAccessibilityViolations,
} from "../support/assertions";

test.describe("Style guide - forms section", () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/style-guide/?alttemplate=styleguideforms");
  });

  test("Validates", async ({ page }) => {
    await expectValidHtml(page, {
      rules: {
        "prefer-native-element": "off", // picks up TinyMCE
        "no-deprecated-attr": "off", // picks up TinyMCE
        "input-missing-label": "off", // not smart enough to recognise aria-label
      },
    });
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
