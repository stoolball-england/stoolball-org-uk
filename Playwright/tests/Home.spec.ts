import { test, expect } from "@playwright/test";
import {
  expectValidHtml,
  expectNoAccessibilityViolations,
} from "../support/assertions";

test.describe("Home page", () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/");
  });

  test("Validates", async ({ page }) => {
    await expectValidHtml(page);
  });

  test("Passes AXE", async ({ page }) => {
    await expectNoAccessibilityViolations(page);
  });

  test("Links to 'what is stoolball?'", async ({ page }) => {
    await page.getByText("Read more about stoolball").first().click();
    await expect(page).toHaveURL(/\/rules\/what-is-stoolball/);
  });
});
