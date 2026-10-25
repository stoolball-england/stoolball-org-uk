import { test, expect } from "@playwright/test";
import {
  expectValidHtml,
  expectNoAccessibilityViolations,
} from "../support/assertions";

test.describe("Reset password", { tag: "@Production" }, () => {
  test.describe("With an invalid token", () => {
    test.beforeEach(async ({ page }) => {
      await page.goto("/account/reset-password/?token=invalid");
    });

    test("Validates", async ({ page }) => {
      await expectValidHtml(page);
    });

    test("Passes AXE", async ({ page }) => {
      await expectNoAccessibilityViolations(page);
    });

    test("Does not reset the password", async ({ page }) => {
      await expect(page.locator("body")).toContainText(
        "Sorry, we can't reset your password.",
      );
    });
  });
});
