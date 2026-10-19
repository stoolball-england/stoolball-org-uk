import { test } from "@playwright/test";
import {
  expectValidHtml,
  expectNoAccessibilityViolations,
} from "../support/assertions";

test.describe("Style guide", () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/style-guide");
  });

  test("Validates", async ({ page }) => {
    await expectValidHtml(page, { rules: { "wcag/h32": "off" } }); // h32 is a technique, not a success criterion. Consent checkboxes are active immediately, which is a different technique.
  });

  test("Passes AXE", async ({ page }) => {
    await expectNoAccessibilityViolations(page);
  });
});
