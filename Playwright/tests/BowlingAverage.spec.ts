import { test } from "@playwright/test";
import {
  expectValidHtml,
  expectNoAccessibilityViolations,
  expectEqualStatisticsShareAPosition,
} from "../support/assertions";

test.describe("Best bowling average", { tag: "@Production" }, () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/play/statistics/bowling-average");
  });

  test("Validates", async ({ page }) => {
    await expectValidHtml(page);
  });

  test("Passes AXE", async ({ page }) => {
    await expectNoAccessibilityViolations(page);
  });

  test("Shows equal values at the same position", async ({ page }) => {
    await expectEqualStatisticsShareAPosition(page);
  });
});
