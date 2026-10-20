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
    await expectValidHtml(page, {
      rules: {
        "heading-level": "off", // the page deliberately has a second <h1> to show the heading of a full news story
        "wcag/h32": "off", // h32 is a technique, not a success criterion. Consent checkboxes are active immediately, which is a different technique.
      },
    });
  });

  test("Passes AXE", async ({ page }) => {
    await expectNoAccessibilityViolations(
      page,
      [],
      [
        "iframe.block__youtube", // the YouTube player's own markup has ARIA errors we can't fix
        'iframe[src^="https://embed.gettyimages.com"]', // the Getty Images embed has its own <main>, which clashes with ours (landmark-unique)
      ],
    );
  });
});
