import { test } from "@playwright/test";
import {
  expectValidHtml,
  expectNoAccessibilityViolations,
} from "../support/assertions";

test.describe("Most scores of 50 or more runs", () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/play/statistics/most-scores-of-50");
  });

  test("Validates", async ({ page }) => {
    await expectValidHtml(page);
  });

  test("Passes AXE", async ({ page }) => {
    await expectNoAccessibilityViolations(page);
  });
});
