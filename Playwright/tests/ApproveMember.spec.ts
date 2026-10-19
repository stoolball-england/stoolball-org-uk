import { test, expect } from "@playwright/test";
import {
  expectValidHtml,
  expectNoAccessibilityViolations,
} from "../support/assertions";

test.describe("Activate member", () => {
  test.describe("Without a key", () => {
    test.beforeEach(async ({ page }) => {
      await page.goto("/account/activate/");
    });

    test("Validates", async ({ page }) => {
      await expectValidHtml(page);
    });

    test("Passes AXE", async ({ page }) => {
      await expectNoAccessibilityViolations(page);
    });

    test("Does not activate a member", async ({ page }) => {
      await expect(page.locator("body")).toContainText(
        "Sorry, we weren't able to activate your account.",
      );
    });
  });
});
