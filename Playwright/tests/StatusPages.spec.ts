import { test } from "@playwright/test";
import {
  expectValidHtml,
  expectNoAccessibilityViolations,
} from "../support/assertions";

// Static pages that IIS serves in place of error responses in production, configured in web.release.config
for (const status of ["400", "404", "410", "500", "offline"]) {
  test.describe(`Status page ${status}`, { tag: "@Production" }, () => {
    test.beforeEach(async ({ page }) => {
      await page.goto(`/status/${status}.html`);
    });

    test("Validates", async ({ page }) => {
      await expectValidHtml(page);
    });

    test("Passes AXE", async ({ page }) => {
      await expectNoAccessibilityViolations(page);
    });
  });
}
