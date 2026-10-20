import { test } from "@playwright/test";
import {
  expectValidHtml,
  expectNoAccessibilityViolations,
} from "../support/assertions";

test.describe("Content page", () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/rules/what-is-stoolball");
  });

  test("Validates", async ({ page }) => {
    await expectValidHtml(page);
  });

  test("Passes AXE", async ({ page }) => {
    await expectNoAccessibilityViolations(
      page,
      [],
      ["iframe.block__youtube"], // the YouTube player's own markup has ARIA errors we can't fix
    );
  });
});
