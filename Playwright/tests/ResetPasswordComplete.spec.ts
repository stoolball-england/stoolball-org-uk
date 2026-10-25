import { test, expect } from "@playwright/test";
import {
  expectValidHtml,
  expectNoAccessibilityViolations,
} from "../support/assertions";

test.describe("Reset password complete", { tag: "@Production" }, () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/account/reset-password/?token=invalid&successful=yes");
  });

  test("Validates", async ({ page }) => {
    await expectValidHtml(page);
  });

  test("Passes AXE", async ({ page }) => {
    await expectNoAccessibilityViolations(page);
  });

  test("Shows the password was reset", async ({ page }) => {
    await expect(page.locator("body")).toContainText(
      "Your new password has been saved",
    );
  });
});
