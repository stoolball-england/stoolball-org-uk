import { test } from "@playwright/test";
import {
  expectValidHtml,
  expectNoAccessibilityViolations,
} from "../support/assertions";

test.describe("Player of the match awards", { tag: "@Production" }, () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/players/rick-mason/player-of-match");
  });

  test("Validates", async ({ page }) => {
    await expectValidHtml(page);
  });

  test("Passes AXE", async ({ page }) => {
    await expectNoAccessibilityViolations(page);
  });
});
